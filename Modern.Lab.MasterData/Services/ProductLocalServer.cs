using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Web.Script.Serialization;

using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.MasterData.Services
{
    internal static class ProductLocalServer
    {
        private const string KeyColumn = "PROD_ID";
        private const int LatencyMs = 120;

        private static readonly object Gate = new object();
        private static readonly DataTable Products = SeedProducts();
        private static readonly DataTable ProductTypes = SeedProductTypes();
        private static readonly DataTable Codes = SeedCodes();

        internal static string Send(string requestText)
        {
            Dictionary<string, object> parsed = ReplyMessageParser.Parse(requestText);
            string name = parsed.ContainsKey(ReplyMessageParser.KeyReplyName)
                    ? Convert.ToString(parsed[ReplyMessageParser.KeyReplyName])
                    : string.Empty;
            string methodCommand = Text(parsed, "MethodCommand");

            Thread.Sleep(LatencyMs);

            lock (Gate)
            {
                if (!string.Equals(name, "ProductAction", StringComparison.Ordinal))
                {
                    return Rejected(name, "UNKNOWN_REQUEST", "Unknown request: " + name);
                }

                switch (methodCommand)
                {
                    case "SelectProducts":
                        return QueryReply(methodCommand, ProductRows(Text(parsed, "Keyword")));

                    case "GetProductTypeList":
                        return QueryReply(methodCommand, TableRows(ProductTypes, null, null));

                    case "GetCodeList":
                        return QueryReply(methodCommand, TableRows(Codes, "GROUP_CD", Text(parsed, "GroupCd")));

                    case "InsertProduct":
                        return Insert(methodCommand, parsed);

                    case "UpdateProduct":
                        return Update(methodCommand, parsed);

                    case "DeleteProduct":
                        return Delete(methodCommand, parsed);

                    default:
                        return Rejected(methodCommand, "UNKNOWN_REQUEST", "Unknown method: " + methodCommand);
                }
            }
        }

        private static string Insert(string name, Dictionary<string, object> fields)
        {
            string key = Text(fields, Param(KeyColumn)).Trim();

            if (key.Length == 0)
            {
                key = NextKey();
            }

            if (FindRow(key) != null)
            {
                return Rejected(name, "DUPLICATE_KEY", "Product '" + key + "' already exists.");
            }

            DataRow row = Products.NewRow();
            Apply(row, fields);
            row[KeyColumn] = key;
            Stamp(row);
            Products.Rows.Add(row);
            PfoNodeLocalServer.EnsureProductNode(key);

            return ActionReply(name, "Product '" + key + "' inserted.", key);
        }

        internal static DataTable Snapshot()
        {
            lock (Gate)
            {
                return Products.Copy();
            }
        }

        private static string NextKey()
        {
            int max = 0;

            foreach (DataRow row in Products.Rows)
            {
                string id = Convert.ToString(row[KeyColumn]);
                int number;

                if (id.StartsWith("PROD-", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(id.Substring(5), NumberStyles.None, CultureInfo.InvariantCulture, out number)
                        && number > max)
                {
                    max = number;
                }
            }

            return "PROD-" + (max + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static string Update(string name, Dictionary<string, object> fields)
        {
            string key = Text(fields, Param(KeyColumn)).Trim();
            DataRow row = FindRow(key);

            if (row == null)
            {
                return Rejected(name, "NOT_FOUND", "Product '" + key + "' was not found.");
            }

            Apply(row, fields);
            Stamp(row);

            return ActionReply(name, "Product '" + key + "' updated.");
        }

        private static string Delete(string name, Dictionary<string, object> fields)
        {
            string key = Text(fields, Param(KeyColumn)).Trim();
            DataRow row = FindRow(key);

            if (row == null)
            {
                return Rejected(name, "NOT_FOUND", "Product '" + key + "' was not found.");
            }

            Products.Rows.Remove(row);

            return ActionReply(name, "Product '" + key + "' deleted.");
        }

        private static string Param(string column)
        {
            return ParameterNameConverter.Convert(column, ParameterNameStyle.PascalCase);
        }

        private static void Apply(DataRow row, Dictionary<string, object> fields)
        {
            foreach (DataColumn column in Products.Columns)
            {
                if (string.Equals(column.ColumnName, KeyColumn, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                object value;

                if (fields.TryGetValue(Param(column.ColumnName), out value))
                {
                    row[column] = Convert.ToString(value) ?? string.Empty;
                }
            }
        }

        private static void Stamp(DataRow row)
        {
            row["UPDATED_BY"] = Environment.UserName;
            row["UPDATED_AT"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static DataRow FindRow(string key)
        {
            foreach (DataRow row in Products.Rows)
            {
                if (string.Equals(Convert.ToString(row[KeyColumn]), key, StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }

            return null;
        }

        private static List<Dictionary<string, object>> ProductRows(string keyword)
        {
            string filter = (keyword ?? string.Empty).Trim();
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            foreach (DataRow row in Products.Rows)
            {
                if (filter.Length > 0
                        && Convert.ToString(row[KeyColumn]).IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                        && Convert.ToString(row["PRODUCT_NAME"]).IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                rows.Add(RowMap(row));
            }

            return rows;
        }

        private static List<Dictionary<string, object>> TableRows(DataTable table, string filterColumn, string filterValue)
        {
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            foreach (DataRow row in table.Rows)
            {
                if (!string.IsNullOrEmpty(filterColumn)
                        && !string.Equals(Convert.ToString(row[filterColumn]), filterValue, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                rows.Add(RowMap(row));
            }

            return rows;
        }

        private static Dictionary<string, object> RowMap(DataRow row)
        {
            Dictionary<string, object> map = new Dictionary<string, object>();

            foreach (DataColumn column in row.Table.Columns)
            {
                map[column.ColumnName] = Convert.ToString(row[column], CultureInfo.InvariantCulture);
            }

            return map;
        }

        private static string QueryReply(string name, List<Dictionary<string, object>> rows)
        {
            string json = new JavaScriptSerializer().Serialize(rows);

            return name + ServerMessageFormat.ReplyNameSuffix
                    + " " + ServerMessageFormat.FieldReturnCode + "=[" + ServerMessageFormat.SuccessReturnCode + "]"
                    + " " + ServerMessageFormat.FieldResultMessage + "=" + json;
        }

        private static string ActionReply(string name, string message)
        {
            return ActionReply(name, message, null);
        }

        private static string ActionReply(string name, string message, string returnedKey)
        {
            string reply = name + ServerMessageFormat.ReplyNameSuffix
                    + " " + ServerMessageFormat.FieldReturnCode + "=[" + ServerMessageFormat.SuccessReturnCode + "]"
                    + " " + ServerMessageFormat.FieldSendMessage + "=[" + Clean(message) + "]";

            if (string.IsNullOrEmpty(returnedKey))
            {
                return reply;
            }

            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            Dictionary<string, object> row = new Dictionary<string, object>();
            row[KeyColumn] = returnedKey;
            rows.Add(row);

            return reply + " " + ServerMessageFormat.FieldResultMessage + "=" + new JavaScriptSerializer().Serialize(rows);
        }

        private static string Rejected(string name, string code, string message)
        {
            return name + ServerMessageFormat.ReplyNameSuffix
                    + " " + ServerMessageFormat.FieldReturnCode + "=[1]"
                    + " " + ServerMessageFormat.FieldErrorCode + "=[" + code + "]"
                    + " " + ServerMessageFormat.FieldErrorMessage + "=[" + Clean(message) + "]";
        }

        private static string Clean(string message)
        {
            return (message ?? string.Empty).Replace(']', ')').Replace('[', '(');
        }

        private static string Text(Dictionary<string, object> fields, string key)
        {
            object value;
            return fields.TryGetValue(key, out value) ? (Convert.ToString(value) ?? string.Empty) : string.Empty;
        }

        private static DataTable SeedProducts()
        {
            DataTable table = new DataTable("PRODUCT");
            table.Columns.Add("PROD_ID");
            table.Columns.Add("PRODUCT_NAME");
            table.Columns.Add("PRODUCT_TYPE");
            table.Columns.Add("UNIT");
            table.Columns.Add("MAKER");
            table.Columns.Add("GRADE");
            table.Columns.Add("STANDARD_QTY");
            table.Columns.Add("SAFETY_STOCK");
            table.Columns.Add("LEAD_TIME_DAYS");
            table.Columns.Add("SPEC_VERSION");
            table.Columns.Add("RELEASE_DATE");
            table.Columns.Add("STORAGE_COND");
            table.Columns.Add("USE_YN");
            table.Columns.Add("DESCRIPTION");
            table.Columns.Add("UPDATED_BY");
            table.Columns.Add("UPDATED_AT");

            table.Rows.Add("PROD-1001", "300mm Bare Wafer", "WAFER", "EA", "SUMCO", "Prime", "25", "50", "14", "1.20", "2026-01-15", "N2 cabinet", "Y", "Prime grade, P-type, <100> orientation.", "admin", "2026-08-01 09:12:00");
            table.Rows.Add("PROD-1002", "200mm Test Wafer", "WAFER", "EA", "SK Siltron", "Test", "25", "25", "7", "1.00", "2025-11-03", "Room", "Y", "Reclaimed test wafer for monitor runs.", "admin", "2026-08-01 09:12:00");
            table.Rows.Add("PROD-2001", "Contact Layer Mask", "MASK", "EA", "Photronics", "A", "1", "1", "30", "3.10", "2026-03-22", "Mask stocker", "Y", "Binary mask, 6025 blank.", "admin", "2026-08-01 09:12:00");
            table.Rows.Add("PROD-2002", "Metal-1 Mask", "MASK", "EA", "Photronics", "A", "1", "0", "30", "2.00", "2025-08-30", "Mask stocker", "N", "Superseded by rev 3 — kept for history.", "admin", "2026-08-01 09:12:00");
            table.Rows.Add("PROD-3001", "Developer TMAH 2.38%", "CHEM", "L", "Dongjin", "Semi", "20", "40", "10", "1.00", "2026-02-10", "Chem room 5C", "Y", string.Empty, "admin", "2026-08-01 09:12:00");
            table.Rows.Add("PROD-3002", "HF 49%", "CHEM", "L", "Soulbrain", "Semi", "20", "20", "10", "1.05", "2026-04-01", "Acid cabinet", "Y", "Handle with acid PPE.", "admin", "2026-08-01 09:12:00");
            table.Rows.Add("PROD-4001", "N2 Process Gas", "GAS", "KG", "Air Liquide", "UHP", "50", "100", "3", "1.00", "2024-12-01", "Gas yard", "Y", string.Empty, "admin", "2026-08-01 09:12:00");
            table.Rows.Add("PROD-5001", "ESC Ceramic Ring", "PART", "EA", "Kyocera", "-", "2", "4", "45", "4.00", "2026-05-18", "Parts shelf B", "Y", "Consumable for etch chamber A.", "admin", "2026-08-01 09:12:00");

            return table;
        }

        private static DataTable SeedProductTypes()
        {
            DataTable table = new DataTable("PRODUCT_TYPE");
            table.Columns.Add("CODE");
            table.Columns.Add("NAME");

            table.Rows.Add("WAFER", "Wafer");
            table.Rows.Add("MASK", "Photo Mask");
            table.Rows.Add("CHEM", "Chemical");
            table.Rows.Add("GAS", "Process Gas");
            table.Rows.Add("PART", "Spare Part");

            return table;
        }

        private static DataTable SeedCodes()
        {
            DataTable table = new DataTable("CODE");
            table.Columns.Add("GROUP_CD");
            table.Columns.Add("CODE");
            table.Columns.Add("NAME");

            table.Rows.Add("UNIT", "EA", "Each");
            table.Rows.Add("UNIT", "LOT", "Lot");
            table.Rows.Add("UNIT", "KG", "Kilogram");
            table.Rows.Add("UNIT", "L", "Liter");
            table.Rows.Add("GRADE", "Prime", "Prime");
            table.Rows.Add("GRADE", "Test", "Test");

            return table;
        }
    }
}
