using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

using Modern.Lab.Hosting;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.MasterData.Services;

namespace Modern.Lab.MasterData
{
    public partial class PfoNodeForm : ModernFormBase
    {
        private const string ChannelLookups = "lookups";
        private const string ChannelNodes = "nodes";
        private const string ProductIdColumn = "PROD_ID";
        private const string ProductNameColumn = "PRODUCT_NAME";
        private const string FlowIdColumn = "FLOW_ID";
        private const string FlowNameColumn = "FLOW_NAME";
        private const string OperIdColumn = "OPER_ID";
        private const string OperNameColumn = "OPER_NAME";

        private static readonly TableContract NodeContract = new TableContract("PfoNode.SelectPfoNodes.Node")
                .Key(PfoNodeLocalServer.NodeIdColumn)
                .Require(PfoNodeLocalServer.FromNodeIdColumn, PfoNodeLocalServer.ToNodeIdColumn,
                        PfoNodeLocalServer.NodeSequenceColumn, PfoNodeLocalServer.ProductProcessColumn,
                        PfoNodeLocalServer.FlowProcessColumn, PfoNodeLocalServer.OperProcessColumn);

        private enum ScreenState
        {
            Loading,
            Ready,
            Editing,
            MissingRoot,
            Blocked,
            Failed,
            Saving
        }

        private ScreenState screenState = ScreenState.Loading;
        private bool bindingProducts;
        private string pendingNodeId;
        private string loadedProductId = string.Empty;
        private TableResponse nodeResponse;
        private DataTable loadedNodeTable = new DataTable();

        public PfoNodeForm()
        {
            this.InitializeComponent();
            this.InitializeModernForm();
            this.DeferredResize = true;

            this.cboProduct.DisplayMember = ProductNameColumn;
            this.cboProduct.ValueMember = ProductIdColumn;
            this.lstFlows.DisplayMember = FlowNameColumn;
            this.lstOpers.DisplayMember = OperNameColumn;
            this.ApplyNativeColors();
            this.UpdateActionState();
        }

        private DataTable RequestTable(string methodCommand, params object[] fields)
        {
            object[] requestFields = WithMethodCommand(methodCommand, fields);
            return this.RequestFields(PfoNodeLocalServer.ActionName, requestFields).Table;
        }

        private DataActionResult RequestAction(string methodCommand, params object[] fields)
        {
            return this.RequestFields(PfoNodeLocalServer.ActionName, WithMethodCommand(methodCommand, fields));
        }

        private static object[] WithMethodCommand(string methodCommand, object[] fields)
        {
            int count = fields == null ? 0 : fields.Length;
            object[] result = new object[count + 2];
            result[0] = "MethodCommand";
            result[1] = methodCommand;

            if (count > 0)
            {
                Array.Copy(fields, 0, result, 2, count);
            }

            return result;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            this.LoadLookups();
        }

        private void LoadLookups()
        {
            this.screenState = ScreenState.Loading;
            this.UpdateActionState();
            this.LoadAsync(ChannelLookups, this.FetchLookups, this.BindLookups, this.AfterLookupSettled);
        }

        private LookupSnapshot FetchLookups()
        {
            DataTable products = this.RequestTable("SelectPfoProducts");
            DataTable flows = this.RequestTable("SelectPfoFlows");
            DataTable opers = this.RequestTable("SelectPfoOpers");
            return new LookupSnapshot(products, flows, opers);
        }

        private void BindLookups(LookupSnapshot snapshot)
        {
            string selected = this.SelectedProductId();
            this.bindingProducts = true;
            this.cboProduct.DataSource = snapshot.Products;
            this.lstFlows.DataSource = snapshot.Flows;
            this.lstOpers.DataSource = snapshot.Opers;

            if (selected.Length > 0)
            {
                this.cboProduct.SelectedValue = selected;
            }

            if (this.cboProduct.SelectedIndex < 0 && snapshot.Products.Rows.Count > 0)
            {
                this.cboProduct.SelectedIndex = 0;
            }

            this.bindingProducts = false;
            this.LoadNodes();
        }

        private void AfterLookupSettled(bool current)
        {
            if (!current || this.cboProduct.DataSource != null)
            {
                return;
            }

            this.screenState = ScreenState.Failed;
            this.ShowStickyNotice("PFO reference data could not be loaded.",
                    Modern.Lab.Controls.Wpf.Display.ToastKind.Error);
            this.UpdateActionState();
        }

        private void OnProductChanged(object sender, EventArgs e)
        {
            if (!this.bindingProducts && this.screenState != ScreenState.Saving
                    && this.screenState != ScreenState.Editing)
            {
                this.LoadNodes();
            }
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            if (this.screenState != ScreenState.Saving && this.screenState != ScreenState.Editing)
            {
                this.LoadNodes();
            }
        }

        private void LoadNodes()
        {
            string productId = this.SelectedProductId();
            this.loadedProductId = productId;
            this.treeNodes.Nodes.Clear();

            if (productId.Length == 0)
            {
                this.screenState = ScreenState.Failed;
                this.UpdateActionState();
                return;
            }

            this.screenState = ScreenState.Loading;
            this.UpdateActionState();
            this.LoadAsync(ChannelNodes,
                    () => this.RequestTable("SelectPfoNodes", PfoNodeLocalServer.ProductProcessColumn, productId),
                    table => this.BindNodes(productId, table),
                    current => this.AfterNodesSettled(productId, current));
        }

        private void BindNodes(string productId, DataTable table)
        {
            if (!string.Equals(productId, this.SelectedProductId(), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            DataTable received = table ?? new DataTable();
            this.nodeResponse = TableResponse.Read(ResponseKind.Data, received, ColumnAliasCatalog.Empty, NodeContract);

            if (this.nodeResponse.State == TableResponseState.MissingRequired
                    || this.nodeResponse.ReceivedColumns.Count == 0)
            {
                this.screenState = ScreenState.Blocked;
                this.treeNodes.Nodes.Clear();
                this.ShowStickyNotice("PFO node response did not match the required columns — writes are disabled.",
                        Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
                this.UpdateActionState();
                return;
            }

            this.HideNotice();
            this.loadedNodeTable = received.Copy();
            this.BuildTree(received);
            this.screenState = this.treeNodes.Nodes.Count == 0 ? ScreenState.MissingRoot : ScreenState.Ready;

            if (this.pendingNodeId != null)
            {
                TreeNode selected = FindTreeNode(this.treeNodes.Nodes, this.pendingNodeId);

                if (selected != null)
                {
                    this.treeNodes.SelectedNode = selected;
                    selected.EnsureVisible();
                }

                this.pendingNodeId = null;
            }

            this.UpdateActionState();
        }

        private void AfterNodesSettled(string productId, bool current)
        {
            if (!current || !string.Equals(productId, this.SelectedProductId(), StringComparison.OrdinalIgnoreCase)
                    || this.screenState != ScreenState.Loading)
            {
                return;
            }

            this.screenState = ScreenState.Failed;
            this.ShowStickyNotice("PFO node structure could not be loaded.",
                    Modern.Lab.Controls.Wpf.Display.ToastKind.Error);
            this.UpdateActionState();
        }

        private void BuildTree(DataTable table)
        {
            this.treeNodes.BeginUpdate();
            this.treeNodes.Nodes.Clear();
            Dictionary<string, TreeNode> byId = new Dictionary<string, TreeNode>(StringComparer.Ordinal);

            foreach (DataRow row in table.Rows)
            {
                NodeItem item = NodeItem.FromRow(row);

                if (item.NodeId.Length == 0)
                {
                    continue;
                }

                TreeNode node = CreateTreeNode(item);
                byId[item.NodeId] = node;
            }

            foreach (KeyValuePair<string, TreeNode> pair in byId)
            {
                NodeItem item = (NodeItem)pair.Value.Tag;
                TreeNode parent;

                if (item.FromNodeId.Length > 0 && byId.TryGetValue(item.FromNodeId, out parent))
                {
                    parent.Nodes.Add(pair.Value);
                }
                else
                {
                    this.treeNodes.Nodes.Add(pair.Value);
                }
            }

            SortNodes(this.treeNodes.Nodes);
            this.treeNodes.ExpandAll();
            this.treeNodes.EndUpdate();
        }

        private static void SortNodes(TreeNodeCollection nodes)
        {
            List<TreeNode> ordered = new List<TreeNode>();

            foreach (TreeNode node in nodes)
            {
                ordered.Add(node);
            }

            ordered.Sort((left, right) => ((NodeItem)left.Tag).NodeSequence.CompareTo(((NodeItem)right.Tag).NodeSequence));
            nodes.Clear();

            foreach (TreeNode node in ordered)
            {
                nodes.Add(node);
                SortNodes(node.Nodes);
            }
        }

        private static TreeNode CreateTreeNode(NodeItem item)
        {
            TreeNode node = new TreeNode(item.Caption);
            node.Name = item.NodeId;
            node.Tag = item;
            return node;
        }

        private string SelectedProductId()
        {
            return (Convert.ToString(this.cboProduct.SelectedValue, CultureInfo.InvariantCulture) ?? string.Empty).Trim();
        }

        private void OnCreateProductNodeClick(object sender, EventArgs e)
        {
            string productId = this.SelectedProductId();

            if (this.screenState != ScreenState.MissingRoot || productId.Length == 0)
            {
                return;
            }

            this.RunWrite(
                    () => this.RequestAction("EnsureProductNode", PfoNodeLocalServer.ProductProcessColumn, productId),
                    "Creating product node…");
        }

        private void OnFlowMouseDown(object sender, MouseEventArgs e)
        {
            this.BeginPaletteDrag(this.lstFlows, e.Location, "Flow", FlowIdColumn);
        }

        private void OnOperMouseDown(object sender, MouseEventArgs e)
        {
            this.BeginPaletteDrag(this.lstOpers, e.Location, "Oper", OperIdColumn);
        }

        private void BeginPaletteDrag(ListBox list, Point location, string kind, string idColumn)
        {
            if (!this.CanEdit)
            {
                return;
            }

            int index = list.IndexFromPoint(location);

            if (index < 0)
            {
                return;
            }

            DataRowView row = list.Items[index] as DataRowView;

            if (row != null)
            {
                list.SelectedIndex = index;
                list.DoDragDrop(new PaletteItem(kind, Convert.ToString(row[idColumn], CultureInfo.InvariantCulture)),
                        DragDropEffects.Copy);
            }
        }

        private void OnTreeItemDrag(object sender, ItemDragEventArgs e)
        {
            TreeNode node = e.Item as TreeNode;
            NodeItem item = node == null ? null : node.Tag as NodeItem;

            if (this.CanEdit && item != null && item.FromNodeId.Length > 0)
            {
                this.treeNodes.DoDragDrop(item, DragDropEffects.Move);
            }
        }

        private void OnTreeDragOver(object sender, DragEventArgs e)
        {
            e.Effect = this.DropAllowed(e) ? (e.Data.GetDataPresent(typeof(NodeItem))
                    ? DragDropEffects.Move : DragDropEffects.Copy) : DragDropEffects.None;
        }

        private void OnTreeDragDrop(object sender, DragEventArgs e)
        {
            if (!this.DropAllowed(e))
            {
                this.ShowToast("Drop a Flow on the Product, or an Oper on a Flow.",
                        Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
                return;
            }

            Point client = this.treeNodes.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = this.treeNodes.GetNodeAt(client);
            PaletteItem palette = e.Data.GetData(typeof(PaletteItem)) as PaletteItem;

            if (palette != null)
            {
                if (string.Equals(palette.Kind, "Flow", StringComparison.Ordinal))
                {
                    this.AddDraftFlow(palette.Id);
                    return;
                }

                this.AddDraftOper(targetNode, palette.Id);
                return;
            }

            NodeItem moving = e.Data.GetData(typeof(NodeItem)) as NodeItem;
            this.MoveDraftNode(moving, targetNode);
        }

        private bool DropAllowed(DragEventArgs e)
        {
            if (!this.CanEdit)
            {
                return false;
            }

            Point client = this.treeNodes.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = this.treeNodes.GetNodeAt(client);
            PaletteItem palette = e.Data.GetData(typeof(PaletteItem)) as PaletteItem;

            if (palette != null)
            {
                if (string.Equals(palette.Kind, "Flow", StringComparison.Ordinal))
                {
                    return this.screenState == ScreenState.MissingRoot
                            || (targetNode != null && ((NodeItem)targetNode.Tag).Kind == NodeKind.Product);
                }

                return targetNode != null && ((NodeItem)targetNode.Tag).Kind == NodeKind.Flow;
            }

            NodeItem moving = e.Data.GetData(typeof(NodeItem)) as NodeItem;
            NodeItem target = targetNode == null ? null : targetNode.Tag as NodeItem;
            return moving != null && target != null && moving.NodeId != target.NodeId
                    && moving.Kind == target.Kind
                    && string.Equals(moving.FromNodeId, target.FromNodeId, StringComparison.Ordinal);
        }

        private void AddDraftFlow(string flowId)
        {
            TreeNode productNode = this.EnsureDraftProductNode();
            NodeItem item = NodeItem.CreateDraft(NodeKind.Flow, flowId, productNode.Name,
                    productNode.Nodes.Count + 1);
            TreeNode node = CreateTreeNode(item);
            productNode.Nodes.Add(node);
            productNode.Expand();
            this.treeNodes.SelectedNode = node;
            this.MarkEditing();
        }

        private void AddDraftOper(TreeNode flowNode, string operId)
        {
            NodeItem item = NodeItem.CreateDraft(NodeKind.Oper, operId, flowNode.Name,
                    flowNode.Nodes.Count + 1);
            TreeNode node = CreateTreeNode(item);
            flowNode.Nodes.Add(node);
            flowNode.Expand();
            this.treeNodes.SelectedNode = node;
            this.MarkEditing();
        }

        private TreeNode EnsureDraftProductNode()
        {
            if (this.treeNodes.Nodes.Count > 0)
            {
                return this.treeNodes.Nodes[0];
            }

            NodeItem item = NodeItem.CreateDraft(NodeKind.Product, this.SelectedProductId(), string.Empty, 0);
            TreeNode node = CreateTreeNode(item);
            this.treeNodes.Nodes.Add(node);
            return node;
        }

        private void MoveDraftNode(NodeItem moving, TreeNode targetNode)
        {
            TreeNode movingNode = FindTreeNode(this.treeNodes.Nodes, moving.NodeId);

            if (movingNode == null || movingNode.Parent == null || targetNode == null || targetNode.Parent == null)
            {
                return;
            }

            TreeNode parent = movingNode.Parent;
            int targetIndex = targetNode.Index;
            movingNode.Remove();

            if (targetIndex > parent.Nodes.Count)
            {
                targetIndex = parent.Nodes.Count;
            }

            parent.Nodes.Insert(targetIndex, movingNode);
            this.Reindex(parent.Nodes);
            this.treeNodes.SelectedNode = movingNode;
            this.MarkEditing();
        }

        private void OnDeleteClick(object sender, EventArgs e)
        {
            TreeNode selected = this.treeNodes.SelectedNode;
            NodeItem item = selected == null ? null : selected.Tag as NodeItem;

            if (item == null || item.Kind == NodeKind.Product || selected.Parent == null || !this.CanEdit)
            {
                return;
            }

            TreeNode parent = selected.Parent;
            selected.Remove();
            this.Reindex(parent.Nodes);
            this.treeNodes.SelectedNode = parent;
            this.MarkEditing();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            if (this.screenState != ScreenState.Editing)
            {
                return;
            }

            this.BuildTree(this.loadedNodeTable.Copy());
            this.screenState = this.treeNodes.Nodes.Count == 0 ? ScreenState.MissingRoot : ScreenState.Ready;
            this.HideNotice();
            this.UpdateActionState();
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            string incompleteFlow;

            if (this.screenState != ScreenState.Editing || !this.DraftValid(out incompleteFlow))
            {
                this.ShowToast("Every Flow needs at least one Oper.",
                        Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
                return;
            }

            string selectedClientId = this.treeNodes.SelectedNode == null
                    ? string.Empty : this.treeNodes.SelectedNode.Name;
            List<Dictionary<string, object>> plan = this.BuildPlan();
            string productId = this.SelectedProductId();
            this.RunWrite(
                    () => this.RequestAction("SavePfoNodes",
                            PfoNodeLocalServer.ProductProcessColumn, productId,
                            "Plan", plan,
                            "SelectedClientId", selectedClientId),
                    "Saving PFO structure…");
        }

        private void OnTreeAfterSelect(object sender, TreeViewEventArgs e)
        {
            this.UpdateActionState();
        }

        private void MarkEditing()
        {
            this.screenState = ScreenState.Editing;
            this.UpdateActionState();
        }

        private void Reindex(TreeNodeCollection nodes)
        {
            for (int index = 0; index < nodes.Count; index = index + 1)
            {
                NodeItem item = (NodeItem)nodes[index].Tag;
                item.SetSequence(index + 1);
                nodes[index].Text = item.Caption;
            }
        }

        private bool DraftValid(out string incompleteFlow)
        {
            incompleteFlow = string.Empty;

            if (this.treeNodes.Nodes.Count == 0)
            {
                return true;
            }

            foreach (TreeNode flowNode in this.treeNodes.Nodes[0].Nodes)
            {
                if (flowNode.Nodes.Count == 0)
                {
                    NodeItem flow = (NodeItem)flowNode.Tag;
                    incompleteFlow = flow.ReferenceId;
                    return false;
                }
            }

            return true;
        }

        private List<Dictionary<string, object>> BuildPlan()
        {
            List<Dictionary<string, object>> plan = new List<Dictionary<string, object>>();

            if (this.treeNodes.Nodes.Count == 0)
            {
                return plan;
            }

            TreeNode product = this.treeNodes.Nodes[0];

            foreach (TreeNode flowNode in product.Nodes)
            {
                plan.Add(PlanRow(flowNode, string.Empty));

                foreach (TreeNode operNode in flowNode.Nodes)
                {
                    plan.Add(PlanRow(operNode, flowNode.Name));
                }
            }

            return plan;
        }

        private static Dictionary<string, object> PlanRow(TreeNode node, string parentClientId)
        {
            NodeItem item = (NodeItem)node.Tag;
            Dictionary<string, object> row = new Dictionary<string, object>();
            row["ClientId"] = item.NodeId;
            row["ParentClientId"] = parentClientId;
            row["NodeId"] = item.IsDraft ? string.Empty : item.NodeId;
            row["Kind"] = item.Kind == NodeKind.Flow ? "Flow" : "Oper";
            row["ReferenceId"] = item.ReferenceId;
            row["NodeSeq"] = item.NodeSequence;
            return row;
        }

        private void RunWrite(Func<DataActionResult> call, string busyText)
        {
            ScreenState previous = this.screenState;
            this.screenState = ScreenState.Saving;
            this.UpdateActionState();
            this.RunAction(call, this.AfterWritten,
                    reply => this.AfterWriteRejected(previous, reply), busyText);
        }

        private void AfterWritten(DataActionResult reply)
        {
            this.pendingNodeId = ReturnedNodeId(reply);
            this.ShowToast(reply.Message.Length > 0 ? reply.Message : "Done.");
            this.LoadNodes();
        }

        private void AfterWriteRejected(ScreenState previous, DataActionResult reply)
        {
            this.screenState = previous;
            this.UpdateActionState();
            this.ShowActionFailure(reply);
        }

        private static string ReturnedNodeId(DataActionResult reply)
        {
            if (reply == null || reply.Data.Length == 0)
            {
                return null;
            }

            string[] values = reply.Values;
            return values.Length == 0 ? null : values[0].Trim();
        }

        private bool CanEdit
        {
            get { return (this.screenState == ScreenState.Ready || this.screenState == ScreenState.MissingRoot
                    || this.screenState == ScreenState.Editing)
                    && !this.ActionInProgress; }
        }

        private void UpdateActionState()
        {
            bool locked = this.screenState == ScreenState.Loading || this.screenState == ScreenState.Saving
                    || this.screenState == ScreenState.Editing;
            this.cboProduct.Enabled = !locked;
            this.btnRefresh.Enabled = !locked && this.cboProduct.DataSource != null;
            this.btnCreateProductNode.Visible = this.screenState == ScreenState.MissingRoot;
            this.btnCreateProductNode.Enabled = this.screenState == ScreenState.MissingRoot && !this.ActionInProgress;
            this.lstFlows.Enabled = this.CanEdit;
            this.lstOpers.Enabled = this.CanEdit && this.treeNodes.Nodes.Count > 0;
            this.treeNodes.AllowDrop = this.CanEdit;
            this.btnCancel.Enabled = this.screenState == ScreenState.Editing && !this.ActionInProgress;
            string incompleteFlow;
            this.btnSave.Enabled = this.screenState == ScreenState.Editing && !this.ActionInProgress
                    && this.DraftValid(out incompleteFlow);
            NodeItem selected = this.treeNodes.SelectedNode == null
                    ? null : this.treeNodes.SelectedNode.Tag as NodeItem;
            this.btnDelete.Enabled = this.CanEdit && selected != null
                    && selected.Kind != NodeKind.Product && !this.ActionInProgress;

            switch (this.screenState)
            {
                case ScreenState.Loading:
                    this.lblState.Text = "Loading PFO structure…";
                    break;
                case ScreenState.MissingRoot:
                    this.lblState.Text = "Product node is missing. Create it or drag a Flow here.";
                    break;
                case ScreenState.Blocked:
                    this.lblState.Text = "The server response is missing required PFO columns.";
                    break;
                case ScreenState.Failed:
                    this.lblState.Text = "PFO structure could not be loaded.";
                    break;
                case ScreenState.Saving:
                    this.lblState.Text = "Saving PFO structure…";
                    break;
                case ScreenState.Editing:
                    string incomplete;
                    this.lblState.Text = this.DraftValid(out incomplete)
                            ? "Draft changed. Save or Cancel."
                            : "Flow " + incomplete + " needs at least one Oper before Save.";
                    break;
                default:
                    this.lblState.Text = "Drag a Flow onto the Product and an Oper onto a Flow. Drag siblings to reorder.";
                    break;
            }
        }

        private void ApplyNativeColors()
        {
            Color background = Modern.Lab.Theming.ModernTheme.Surface;
            Color foreground = Modern.Lab.Theming.ModernTheme.TextPrimary;
            this.lstFlows.BackColor = background;
            this.lstFlows.ForeColor = foreground;
            this.lstOpers.BackColor = background;
            this.lstOpers.ForeColor = foreground;
            this.treeNodes.BackColor = background;
            this.treeNodes.ForeColor = foreground;
        }

        private static TreeNode FindTreeNode(TreeNodeCollection nodes, string nodeId)
        {
            foreach (TreeNode node in nodes)
            {
                NodeItem item = node.Tag as NodeItem;

                if (item != null && string.Equals(item.NodeId, nodeId, StringComparison.Ordinal))
                {
                    return node;
                }

                TreeNode found = FindTreeNode(node.Nodes, nodeId);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private sealed class LookupSnapshot
        {
            internal LookupSnapshot(DataTable products, DataTable flows, DataTable opers)
            {
                this.Products = products ?? new DataTable();
                this.Flows = flows ?? new DataTable();
                this.Opers = opers ?? new DataTable();
            }

            internal DataTable Products { get; private set; }

            internal DataTable Flows { get; private set; }

            internal DataTable Opers { get; private set; }
        }

        private enum NodeKind
        {
            Product,
            Flow,
            Oper
        }

        private sealed class NodeItem
        {
            internal string NodeId { get; private set; }

            internal string FromNodeId { get; private set; }

            internal int NodeSequence { get; private set; }

            internal NodeKind Kind { get; private set; }

            internal string Caption { get; private set; }

            internal string ReferenceId { get; private set; }

            internal bool IsDraft { get; private set; }

            internal static NodeItem FromRow(DataRow row)
            {
                string product = Text(row, PfoNodeLocalServer.ProductProcessColumn);
                string flow = Text(row, PfoNodeLocalServer.FlowProcessColumn);
                string oper = Text(row, PfoNodeLocalServer.OperProcessColumn);
                NodeKind kind = product.Length > 0 ? NodeKind.Product : (flow.Length > 0 ? NodeKind.Flow : NodeKind.Oper);
                int sequence;
                int.TryParse(Text(row, PfoNodeLocalServer.NodeSequenceColumn), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out sequence);
                string value = product.Length > 0 ? product : (flow.Length > 0 ? flow : oper);
                string prefix = kind == NodeKind.Product ? "Product" : (kind == NodeKind.Flow ? "Flow" : "Oper");

                return new NodeItem
                {
                    NodeId = Text(row, PfoNodeLocalServer.NodeIdColumn),
                    FromNodeId = Text(row, PfoNodeLocalServer.FromNodeIdColumn),
                    NodeSequence = sequence,
                    Kind = kind,
                    Caption = kind == NodeKind.Product ? prefix + "  " + value
                            : sequence.ToString(CultureInfo.InvariantCulture) + ". " + prefix + "  " + value,
                    ReferenceId = value,
                    IsDraft = false
                };
            }

            internal static NodeItem CreateDraft(NodeKind kind, string referenceId, string fromNodeId, int sequence)
            {
                NodeItem item = new NodeItem
                {
                    NodeId = "draft-" + Guid.NewGuid().ToString("N"),
                    FromNodeId = fromNodeId,
                    NodeSequence = sequence,
                    Kind = kind,
                    ReferenceId = referenceId,
                    IsDraft = true
                };
                item.UpdateCaption();
                return item;
            }

            internal void SetSequence(int sequence)
            {
                this.NodeSequence = sequence;
                this.UpdateCaption();
            }

            private void UpdateCaption()
            {
                string prefix = this.Kind == NodeKind.Product ? "Product"
                        : (this.Kind == NodeKind.Flow ? "Flow" : "Oper");
                this.Caption = this.Kind == NodeKind.Product ? prefix + "  " + this.ReferenceId
                        : this.NodeSequence.ToString(CultureInfo.InvariantCulture) + ". " + prefix + "  "
                                + this.ReferenceId;
            }

            private static string Text(DataRow row, string column)
            {
                return row.Table.Columns.Contains(column)
                        ? (Convert.ToString(row[column], CultureInfo.InvariantCulture) ?? string.Empty).Trim()
                        : string.Empty;
            }
        }

        private sealed class PaletteItem
        {
            internal PaletteItem(string kind, string id)
            {
                this.Kind = kind;
                this.Id = id;
            }

            internal string Kind { get; private set; }

            internal string Id { get; private set; }
        }
    }
}
