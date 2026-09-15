using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;

using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.MasterData.Services
{
    internal static class FlowLocalServer
    {
        private const string KeyColumn = "FLOW_ID";
        private const string ActionName = "FlowAction";
        private const int LatencyMs = 120;

        private static readonly object Gate = new object();
        private static readonly DataTable Flows = SeedFlows();

        internal static string Send(string requestText)
        {
            Dictionary<string, object> parsed = ReplyMessageParser.Parse(requestText);
            string name = parsed.ContainsKey(ReplyMessageParser.KeyReplyName)
                    ? Convert.ToString(parsed[ReplyMessageParser.KeyReplyName])
                    : string.Empty;
            string methodCommand = MasterDataLocalReply.Text(parsed, "MethodCommand");

            Thread.Sleep(LatencyMs);

            lock (Gate)
            {
                if (!string.Equals(name, ActionName, StringComparison.Ordinal))
                {
                    return MasterDataLocalReply.Rejected(name, "UNKNOWN_REQUEST", "Unknown request: " + name);
                }

                switch (methodCommand)
                {
                    case "SelectFlows":
                        return MasterDataLocalReply.QueryReply(
                                methodCommand,
                                MasterDataLocalReply.Matching(Flows, MasterDataLocalReply.Text(parsed, "Keyword")));

                    case "InsertFlow":
                        return Insert(methodCommand, parsed);

                    case "UpdateFlow":
                        return Update(methodCommand, parsed);

                    case "DeleteFlow":
                        return Delete(methodCommand, parsed);

                    default:
                        return MasterDataLocalReply.Rejected(methodCommand, "UNKNOWN_REQUEST", "Unknown method: " + methodCommand);
                }
            }
        }

        private static string Insert(string name, Dictionary<string, object> fields)
        {
            string key = MasterDataLocalReply.Text(fields, MasterDataLocalReply.Param(KeyColumn)).Trim();

            if (key.Length == 0)
            {
                return MasterDataLocalReply.Rejected(name, "EMPTY_KEY", "Flow Id is required.");
            }

            if (FindRow(key) != null)
            {
                return MasterDataLocalReply.Rejected(name, "DUPLICATE_KEY", "Flow '" + key + "' already exists.");
            }

            DataRow row = Flows.NewRow();
            MasterDataLocalReply.Apply(Flows, row, fields, KeyColumn);
            row[KeyColumn] = key;
            Flows.Rows.Add(row);

            return MasterDataLocalReply.ActionReply(name, "Flow '" + key + "' inserted.", KeyColumn, key);
        }

        private static string Update(string name, Dictionary<string, object> fields)
        {
            string key = MasterDataLocalReply.Text(fields, MasterDataLocalReply.Param(KeyColumn)).Trim();
            DataRow row = FindRow(key);

            if (row == null)
            {
                return MasterDataLocalReply.Rejected(name, "NOT_FOUND", "Flow '" + key + "' was not found.");
            }

            MasterDataLocalReply.Apply(Flows, row, fields, KeyColumn);

            return MasterDataLocalReply.ActionReply(name, "Flow '" + key + "' updated.");
        }

        private static string Delete(string name, Dictionary<string, object> fields)
        {
            string key = MasterDataLocalReply.Text(fields, MasterDataLocalReply.Param(KeyColumn)).Trim();
            DataRow row = FindRow(key);

            if (row == null)
            {
                return MasterDataLocalReply.Rejected(name, "NOT_FOUND", "Flow '" + key + "' was not found.");
            }

            Flows.Rows.Remove(row);

            return MasterDataLocalReply.ActionReply(name, "Flow '" + key + "' deleted.");
        }

        private static DataRow FindRow(string key)
        {
            foreach (DataRow row in Flows.Rows)
            {
                if (string.Equals(Convert.ToString(row[KeyColumn], CultureInfo.InvariantCulture), key, StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }

            return null;
        }

        internal static DataTable Snapshot()
        {
            lock (Gate)
            {
                return Flows.Copy();
            }
        }

        private static DataTable SeedFlows()
        {
            DataTable table = new DataTable("FLOW");
            table.Columns.Add(KeyColumn);
            table.Columns.Add("FLOW_NAME");
            table.Columns.Add("FLOW_VERSION");
            table.Columns.Add("STEP_COUNT");
            table.Columns.Add("OWNER");
            table.Columns.Add("USE_YN");
            table.Columns.Add("REMARK");

            table.Rows.Add("FLW-CMOS-28", "CMOS 28nm Logic", "4.2", "182", "Process Integration", "Y", "Baseline logic flow.");
            table.Rows.Add("FLW-CMOS-14", "CMOS 14nm Logic", "2.0", "241", "Process Integration", "Y", string.Empty);
            table.Rows.Add("FLW-DRAM-1Z", "DRAM 1z Node", "1.7", "205", "Memory Integration", "Y", "Capacitor module revised.");
            table.Rows.Add("FLW-MON-OX", "Oxide Monitor", "1.0", "12", "Defect Metrology", "Y", "Monitor wafers only.");
            table.Rows.Add("FLW-MON-PR", "Photoresist Monitor", "1.1", "9", "Litho Metrology", "N", "Retired after PR change.");
            table.Rows.Add("FLW-REWORK-LI", "Litho Rework", "3.0", "6", "Litho", "Y", "Coat strip and recoat.");

            return table;
        }
    }
}
