using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;

using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.MasterData.Services
{
    internal static class OperLocalServer
    {
        private const string KeyColumn = "OPER_ID";
        private const string ActionName = "OperAction";
        private const int LatencyMs = 120;

        private static readonly object Gate = new object();
        private static readonly DataTable Opers = SeedOpers();

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
                    case "SelectOpers":
                        return MasterDataLocalReply.QueryReply(
                                methodCommand,
                                MasterDataLocalReply.Matching(Opers, MasterDataLocalReply.Text(parsed, "Keyword")));

                    case "InsertOper":
                        return Insert(methodCommand, parsed);

                    case "UpdateOper":
                        return Update(methodCommand, parsed);

                    case "DeleteOper":
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
                return MasterDataLocalReply.Rejected(name, "EMPTY_KEY", "Oper Id is required.");
            }

            if (FindRow(key) != null)
            {
                return MasterDataLocalReply.Rejected(name, "DUPLICATE_KEY", "Oper '" + key + "' already exists.");
            }

            DataRow row = Opers.NewRow();
            MasterDataLocalReply.Apply(Opers, row, fields, KeyColumn);
            row[KeyColumn] = key;
            Opers.Rows.Add(row);

            return MasterDataLocalReply.ActionReply(name, "Oper '" + key + "' inserted.", KeyColumn, key);
        }

        private static string Update(string name, Dictionary<string, object> fields)
        {
            string key = MasterDataLocalReply.Text(fields, MasterDataLocalReply.Param(KeyColumn)).Trim();
            DataRow row = FindRow(key);

            if (row == null)
            {
                return MasterDataLocalReply.Rejected(name, "NOT_FOUND", "Oper '" + key + "' was not found.");
            }

            MasterDataLocalReply.Apply(Opers, row, fields, KeyColumn);

            return MasterDataLocalReply.ActionReply(name, "Oper '" + key + "' updated.");
        }

        private static string Delete(string name, Dictionary<string, object> fields)
        {
            string key = MasterDataLocalReply.Text(fields, MasterDataLocalReply.Param(KeyColumn)).Trim();
            DataRow row = FindRow(key);

            if (row == null)
            {
                return MasterDataLocalReply.Rejected(name, "NOT_FOUND", "Oper '" + key + "' was not found.");
            }

            Opers.Rows.Remove(row);

            return MasterDataLocalReply.ActionReply(name, "Oper '" + key + "' deleted.");
        }

        private static DataRow FindRow(string key)
        {
            foreach (DataRow row in Opers.Rows)
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
                return Opers.Copy();
            }
        }

        private static DataTable SeedOpers()
        {
            DataTable table = new DataTable("OPER");
            table.Columns.Add(KeyColumn);
            table.Columns.Add("OPER_NAME");
            table.Columns.Add("OPER_GROUP");
            table.Columns.Add("STD_TIME_MIN");
            table.Columns.Add("EQP_TYPE");
            table.Columns.Add("USE_YN");
            table.Columns.Add("REMARK");

            table.Rows.Add("OP-1010", "Pad Oxide Growth", "Diffusion", "95", "FURNACE", "Y", "Dry oxidation.");
            table.Rows.Add("OP-1020", "Nitride Deposition", "Thin Film", "48", "LPCVD", "Y", string.Empty);
            table.Rows.Add("OP-2010", "Active Litho", "Photo", "22", "SCANNER", "Y", "ArF immersion.");
            table.Rows.Add("OP-2020", "Active Etch", "Etch", "31", "ETCHER", "Y", string.Empty);
            table.Rows.Add("OP-3010", "Well Implant", "Implant", "18", "IMPLANTER", "Y", "Boron, 180keV.");
            table.Rows.Add("OP-4010", "CMP Oxide", "CMP", "27", "POLISHER", "N", "Replaced by OP-4011.");
            table.Rows.Add("OP-9010", "Defect Inspection", "Metrology", "14", "INSPECTOR", "Y", "Sampling 2 of 25.");

            return table;
        }
    }
}
