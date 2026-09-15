using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Web.Script.Serialization;

using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.MasterData.Services
{
    internal static class PfoNodeLocalServer
    {
        internal const string ActionName = "PfoNodeAction";
        internal const string NodeIdColumn = "NODE_ID";
        internal const string FromNodeIdColumn = "FR_NODE_ID";
        internal const string ToNodeIdColumn = "TO_NODE_ID";
        internal const string NodeSequenceColumn = "NODE_SEQ";
        internal const string ProductProcessColumn = "ProductProcess";
        internal const string FlowProcessColumn = "FlowProcess";
        internal const string OperProcessColumn = "OperProcess";

        private const int LatencyMs = 120;
        private static readonly object Gate = new object();
        private static readonly List<NodeRecord> Nodes = new List<NodeRecord>();
        private static readonly List<RelationRecord> Relations = new List<RelationRecord>();
        private static long lastNodeKey;
        private static int saveRequestCount;

        static PfoNodeLocalServer()
        {
            Seed();
        }

        internal static string Send(string requestText)
        {
            Dictionary<string, object> parsed = ReplyMessageParser.Parse(requestText);
            string name = parsed.ContainsKey(ReplyMessageParser.KeyReplyName)
                    ? Convert.ToString(parsed[ReplyMessageParser.KeyReplyName], CultureInfo.InvariantCulture)
                    : string.Empty;
            string methodCommand = MasterDataLocalReply.Text(parsed, "MethodCommand");

            Thread.Sleep(LatencyMs);

            if (!string.Equals(name, ActionName, StringComparison.Ordinal))
            {
                return MasterDataLocalReply.Rejected(name, "UNKNOWN_REQUEST", "Unknown request: " + name);
            }

            if (string.Equals(methodCommand, "SelectPfoProducts", StringComparison.Ordinal))
            {
                return MasterDataLocalReply.QueryReply(methodCommand,
                        MasterDataLocalReply.Matching(ProductLocalServer.Snapshot(), string.Empty));
            }

            if (string.Equals(methodCommand, "SelectPfoFlows", StringComparison.Ordinal))
            {
                return MasterDataLocalReply.QueryReply(methodCommand,
                        MasterDataLocalReply.Matching(FlowLocalServer.Snapshot(), string.Empty));
            }

            if (string.Equals(methodCommand, "SelectPfoOpers", StringComparison.Ordinal))
            {
                return MasterDataLocalReply.QueryReply(methodCommand,
                        MasterDataLocalReply.Matching(OperLocalServer.Snapshot(), string.Empty));
            }

            string referenceError = ValidateReferences(methodCommand, parsed);

            if (referenceError.Length > 0)
            {
                return referenceError;
            }

            lock (Gate)
            {
                switch (methodCommand)
                {
                    case "SelectPfoNodes":
                        return SelectNodes(methodCommand, MasterDataLocalReply.Text(parsed, ProductProcessColumn));

                    case "EnsureProductNode":
                        return EnsureProduct(methodCommand, MasterDataLocalReply.Text(parsed, ProductProcessColumn));

                    case "SavePfoNodes":
                        saveRequestCount = saveRequestCount + 1;
                        return SavePlan(methodCommand, parsed);

                    default:
                        return MasterDataLocalReply.Rejected(methodCommand, "UNKNOWN_REQUEST", "Unknown method: " + methodCommand);
                }
            }
        }

        internal static string EnsureProductNode(string productId)
        {
            lock (Gate)
            {
                NodeRecord existing = FindProduct(productId);

                if (existing != null)
                {
                    return existing.NodeId;
                }

                NodeRecord created = new NodeRecord(NextNodeId(), productId, string.Empty, string.Empty);
                Nodes.Add(created);
                return created.NodeId;
            }
        }

        internal static int SaveRequestCount
        {
            get
            {
                lock (Gate)
                {
                    return saveRequestCount;
                }
            }
        }

        private static string SelectNodes(string name, string productId)
        {
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            NodeRecord product = FindProduct(productId);

            if (product == null)
            {
                rows.Add(NodeMap(null, null));
                return MasterDataLocalReply.QueryReply(name, rows);
            }

            rows.Add(NodeMap(product, null));

            foreach (RelationRecord flowRelation in Children(product.NodeId))
            {
                NodeRecord flow = FindNode(flowRelation.ToNodeId);

                if (flow == null)
                {
                    continue;
                }

                rows.Add(NodeMap(flow, flowRelation));

                foreach (RelationRecord operRelation in Children(flow.NodeId))
                {
                    NodeRecord oper = FindNode(operRelation.ToNodeId);

                    if (oper != null)
                    {
                        rows.Add(NodeMap(oper, operRelation));
                    }
                }
            }

            return MasterDataLocalReply.QueryReply(name, rows);
        }

        private static string EnsureProduct(string name, string productId)
        {
            string nodeId = EnsureProductNode(productId);
            return MasterDataLocalReply.ActionReply(name, "Product node is ready.", NodeIdColumn, nodeId);
        }

        private static string SavePlan(string name, Dictionary<string, object> fields)
        {
            string productId = MasterDataLocalReply.Text(fields, ProductProcessColumn).Trim();
            string selectedClientId = MasterDataLocalReply.Text(fields, "SelectedClientId").Trim();
            List<DraftRecord> plan;
            string parseError;

            if (!TryReadPlan(MasterDataLocalReply.Text(fields, "Plan"), out plan, out parseError))
            {
                return MasterDataLocalReply.Rejected(name, "INVALID_PLAN", parseError);
            }

            NodeRecord product = FindProduct(productId);
            HashSet<string> existingNodeIds = product == null
                    ? new HashSet<string>(StringComparer.Ordinal)
                    : DescendantIds(product.NodeId);
            string validationError = ValidatePlan(plan, existingNodeIds);

            if (validationError.Length > 0)
            {
                return MasterDataLocalReply.Rejected(name, "INVALID_PLAN", validationError);
            }

            List<NodeRecord> replacementNodes = Nodes.FindAll(node => !existingNodeIds.Contains(node.NodeId));
            List<RelationRecord> replacementRelations = Relations.FindAll(relation =>
                    !existingNodeIds.Contains(relation.FromNodeId)
                    && !existingNodeIds.Contains(relation.ToNodeId));

            if (product == null)
            {
                product = new NodeRecord(NextNodeId(), productId, string.Empty, string.Empty);
                replacementNodes.Add(product);
            }

            Dictionary<string, string> nodeIds = new Dictionary<string, string>(StringComparer.Ordinal);
            List<DraftRecord> flows = plan.FindAll(item => string.Equals(item.Kind, "Flow", StringComparison.Ordinal));
            flows.Sort((left, right) => left.NodeSequence.CompareTo(right.NodeSequence));

            for (int flowIndex = 0; flowIndex < flows.Count; flowIndex = flowIndex + 1)
            {
                DraftRecord flowDraft = flows[flowIndex];
                string flowNodeId = flowDraft.NodeId.Length == 0 ? NextNodeId() : flowDraft.NodeId;
                nodeIds[flowDraft.ClientId] = flowNodeId;
                replacementNodes.Add(new NodeRecord(flowNodeId, string.Empty, flowDraft.ReferenceId, string.Empty));
                replacementRelations.Add(new RelationRecord(product.NodeId, flowNodeId, flowIndex + 1));

                List<DraftRecord> opers = plan.FindAll(item =>
                        string.Equals(item.Kind, "Oper", StringComparison.Ordinal)
                        && string.Equals(item.ParentClientId, flowDraft.ClientId, StringComparison.Ordinal));
                opers.Sort((left, right) => left.NodeSequence.CompareTo(right.NodeSequence));

                for (int operIndex = 0; operIndex < opers.Count; operIndex = operIndex + 1)
                {
                    DraftRecord operDraft = opers[operIndex];
                    string operNodeId = operDraft.NodeId.Length == 0 ? NextNodeId() : operDraft.NodeId;
                    nodeIds[operDraft.ClientId] = operNodeId;
                    replacementNodes.Add(new NodeRecord(operNodeId, string.Empty, string.Empty, operDraft.ReferenceId));
                    replacementRelations.Add(new RelationRecord(flowNodeId, operNodeId, operIndex + 1));
                }
            }

            Nodes.Clear();
            Nodes.AddRange(replacementNodes);
            Relations.Clear();
            Relations.AddRange(replacementRelations);

            string selectedNodeId;

            if (!nodeIds.TryGetValue(selectedClientId, out selectedNodeId))
            {
                selectedNodeId = product.NodeId;
            }

            return MasterDataLocalReply.ActionReply(name, "PFO structure saved.", NodeIdColumn, selectedNodeId);
        }

        private static bool TryReadPlan(string json, out List<DraftRecord> plan, out string error)
        {
            plan = new List<DraftRecord>();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                return true;
            }

            try
            {
                List<Dictionary<string, object>> rows =
                        new JavaScriptSerializer().Deserialize<List<Dictionary<string, object>>>(json);

                foreach (Dictionary<string, object> row in rows)
                {
                    int sequence;

                    if (!int.TryParse(MasterDataLocalReply.Text(row, "NodeSeq"), NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out sequence))
                    {
                        error = "Every plan row needs NodeSeq.";
                        return false;
                    }

                    plan.Add(new DraftRecord(
                            MasterDataLocalReply.Text(row, "ClientId").Trim(),
                            MasterDataLocalReply.Text(row, "ParentClientId").Trim(),
                            MasterDataLocalReply.Text(row, "NodeId").Trim(),
                            MasterDataLocalReply.Text(row, "Kind").Trim(),
                            MasterDataLocalReply.Text(row, "ReferenceId").Trim(),
                            sequence));
                }

                return true;
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }
            catch (ArgumentException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static string ValidatePlan(List<DraftRecord> plan, HashSet<string> existingNodeIds)
        {
            HashSet<string> clientIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> usedNodeIds = new HashSet<string>(StringComparer.Ordinal);
            DataTable flowTable = FlowLocalServer.Snapshot();
            DataTable operTable = OperLocalServer.Snapshot();

            foreach (DraftRecord item in plan)
            {
                if (item.ClientId.Length == 0 || !clientIds.Add(item.ClientId))
                {
                    return "ClientId must be present and unique.";
                }

                if (item.NodeSequence < 1)
                {
                    return "NodeSeq must start at 1.";
                }

                if (item.NodeId.Length > 0
                        && (!existingNodeIds.Contains(item.NodeId) || !usedNodeIds.Add(item.NodeId)))
                {
                    return "An existing NodeId does not belong to the selected Product.";
                }

                NodeRecord existing = item.NodeId.Length == 0 ? null : FindNode(item.NodeId);

                if (string.Equals(item.Kind, "Flow", StringComparison.Ordinal))
                {
                    if (item.ParentClientId.Length > 0 || !Exists(flowTable, "FLOW_ID", item.ReferenceId))
                    {
                        return "A Flow reference or parent is invalid.";
                    }

                    if (existing != null && !string.Equals(existing.FlowProcess, item.ReferenceId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return "An existing Flow Node cannot change its reference.";
                    }
                }
                else if (string.Equals(item.Kind, "Oper", StringComparison.Ordinal))
                {
                    if (item.ParentClientId.Length == 0 || !Exists(operTable, "OPER_ID", item.ReferenceId))
                    {
                        return "An Oper reference or parent is invalid.";
                    }

                    if (existing != null && !string.Equals(existing.OperProcess, item.ReferenceId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return "An existing Oper Node cannot change its reference.";
                    }
                }
                else
                {
                    return "Only Flow and Oper rows are allowed.";
                }
            }

            foreach (DraftRecord flow in plan.FindAll(item => string.Equals(item.Kind, "Flow", StringComparison.Ordinal)))
            {
                int childCount = plan.FindAll(item => string.Equals(item.Kind, "Oper", StringComparison.Ordinal)
                        && string.Equals(item.ParentClientId, flow.ClientId, StringComparison.Ordinal)).Count;

                if (childCount == 0)
                {
                    return "Every Flow needs at least one Oper.";
                }
            }

            foreach (DraftRecord oper in plan.FindAll(item => string.Equals(item.Kind, "Oper", StringComparison.Ordinal)))
            {
                DraftRecord parent = plan.Find(item =>
                        string.Equals(item.ClientId, oper.ParentClientId, StringComparison.Ordinal));

                if (parent == null || !string.Equals(parent.Kind, "Flow", StringComparison.Ordinal))
                {
                    return "Every Oper must belong to a Flow in the same plan.";
                }
            }

            List<DraftRecord> flowRows = plan.FindAll(item => string.Equals(item.Kind, "Flow", StringComparison.Ordinal));

            if (!HasContinuousSequence(flowRows))
            {
                return "Flow NodeSeq must be continuous from 1.";
            }

            foreach (DraftRecord flow in flowRows)
            {
                List<DraftRecord> operRows = plan.FindAll(item => string.Equals(item.Kind, "Oper", StringComparison.Ordinal)
                        && string.Equals(item.ParentClientId, flow.ClientId, StringComparison.Ordinal));

                if (!HasContinuousSequence(operRows))
                {
                    return "Oper NodeSeq must be continuous from 1.";
                }
            }

            return string.Empty;
        }

        private static bool HasContinuousSequence(List<DraftRecord> rows)
        {
            rows.Sort((left, right) => left.NodeSequence.CompareTo(right.NodeSequence));

            for (int index = 0; index < rows.Count; index = index + 1)
            {
                if (rows[index].NodeSequence != index + 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static HashSet<string> DescendantIds(string parentId)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            AddDescendants(parentId, result);
            return result;
        }

        private static void AddDescendants(string parentId, HashSet<string> result)
        {
            foreach (RelationRecord relation in Children(parentId))
            {
                if (result.Add(relation.ToNodeId))
                {
                    AddDescendants(relation.ToNodeId, result);
                }
            }
        }

        private static string ValidateReferences(string methodCommand, Dictionary<string, object> fields)
        {
            if (string.Equals(methodCommand, "EnsureProductNode", StringComparison.Ordinal)
                    || string.Equals(methodCommand, "SavePfoNodes", StringComparison.Ordinal))
            {
                string productId = MasterDataLocalReply.Text(fields, ProductProcessColumn).Trim();

                if (!Exists(ProductLocalServer.Snapshot(), "PROD_ID", productId))
                {
                    return MasterDataLocalReply.Rejected(methodCommand, "PRODUCT_NOT_FOUND",
                            "Product '" + productId + "' was not found.");
                }
            }

            return string.Empty;
        }

        private static Dictionary<string, object> NodeMap(NodeRecord node, RelationRecord relation)
        {
            Dictionary<string, object> map = new Dictionary<string, object>();
            map[NodeIdColumn] = node == null ? string.Empty : node.NodeId;
            map[FromNodeIdColumn] = relation == null ? string.Empty : relation.FromNodeId;
            map[ToNodeIdColumn] = node == null ? string.Empty : node.NodeId;
            map[NodeSequenceColumn] = relation == null ? 0 : relation.NodeSequence;
            map[ProductProcessColumn] = node == null ? string.Empty : node.ProductProcess;
            map[FlowProcessColumn] = node == null ? string.Empty : node.FlowProcess;
            map[OperProcessColumn] = node == null ? string.Empty : node.OperProcess;
            return map;
        }

        private static List<RelationRecord> Children(string parentId)
        {
            List<RelationRecord> result = Relations.FindAll(
                    item => string.Equals(item.FromNodeId, parentId, StringComparison.Ordinal));
            result.Sort((left, right) => left.NodeSequence.CompareTo(right.NodeSequence));
            return result;
        }

        private static NodeRecord FindProduct(string productId)
        {
            return Nodes.Find(item => string.Equals(item.ProductProcess, productId, StringComparison.OrdinalIgnoreCase));
        }

        private static NodeRecord FindNode(string nodeId)
        {
            return Nodes.Find(item => string.Equals(item.NodeId, nodeId, StringComparison.Ordinal));
        }

        private static bool Exists(DataTable table, string column, string value)
        {
            foreach (DataRow row in table.Rows)
            {
                if (string.Equals(Convert.ToString(row[column], CultureInfo.InvariantCulture), value,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NextNodeId()
        {
            long candidate = DateTime.UtcNow.Ticks;

            if (candidate <= lastNodeKey)
            {
                candidate = lastNodeKey + 1;
            }

            lastNodeKey = candidate;
            return candidate.ToString("D20", CultureInfo.InvariantCulture);
        }

        private static void Seed()
        {
            DataTable products = ProductLocalServer.Snapshot();

            foreach (DataRow row in products.Rows)
            {
                EnsureProductNode(Convert.ToString(row["PROD_ID"], CultureInfo.InvariantCulture));
            }

            NodeRecord product = FindProduct("PROD-1001");

            if (product == null)
            {
                return;
            }

            NodeRecord flow = new NodeRecord(NextNodeId(), string.Empty, "FLW-CMOS-28", string.Empty);
            NodeRecord firstOper = new NodeRecord(NextNodeId(), string.Empty, string.Empty, "OP-1010");
            NodeRecord secondOper = new NodeRecord(NextNodeId(), string.Empty, string.Empty, "OP-1020");
            Nodes.Add(flow);
            Nodes.Add(firstOper);
            Nodes.Add(secondOper);
            Relations.Add(new RelationRecord(product.NodeId, flow.NodeId, 1));
            Relations.Add(new RelationRecord(flow.NodeId, firstOper.NodeId, 1));
            Relations.Add(new RelationRecord(flow.NodeId, secondOper.NodeId, 2));
        }

        private sealed class DraftRecord
        {
            internal DraftRecord(string clientId, string parentClientId, string nodeId, string kind,
                    string referenceId, int nodeSequence)
            {
                this.ClientId = clientId;
                this.ParentClientId = parentClientId;
                this.NodeId = nodeId;
                this.Kind = kind;
                this.ReferenceId = referenceId;
                this.NodeSequence = nodeSequence;
            }

            internal string ClientId { get; private set; }

            internal string ParentClientId { get; private set; }

            internal string NodeId { get; private set; }

            internal string Kind { get; private set; }

            internal string ReferenceId { get; private set; }

            internal int NodeSequence { get; private set; }
        }

        private sealed class NodeRecord
        {
            internal NodeRecord(string nodeId, string productProcess, string flowProcess, string operProcess)
            {
                this.NodeId = nodeId;
                this.ProductProcess = productProcess;
                this.FlowProcess = flowProcess;
                this.OperProcess = operProcess;
            }

            internal string NodeId { get; private set; }

            internal string ProductProcess { get; private set; }

            internal string FlowProcess { get; private set; }

            internal string OperProcess { get; private set; }
        }

        private sealed class RelationRecord
        {
            internal RelationRecord(string fromNodeId, string toNodeId, int nodeSequence)
            {
                this.FromNodeId = fromNodeId;
                this.ToNodeId = toNodeId;
                this.NodeSequence = nodeSequence;
            }

            internal string FromNodeId { get; private set; }

            internal string ToNodeId { get; private set; }

            internal int NodeSequence { get; set; }
        }
    }
}
