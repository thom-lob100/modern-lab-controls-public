using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web.Script.Serialization;

using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.MasterData.Services
{
    internal static class MasterDataLocalReply
    {
        internal static string Param(string column)
        {
            return ParameterNameConverter.Convert(column, ParameterNameStyle.PascalCase);
        }

        internal static string Text(Dictionary<string, object> fields, string key)
        {
            object value;
            return fields.TryGetValue(key, out value) ? (Convert.ToString(value) ?? string.Empty) : string.Empty;
        }

        internal static void Apply(DataTable table, DataRow row, Dictionary<string, object> fields, string keyColumn)
        {
            foreach (DataColumn column in table.Columns)
            {
                if (string.Equals(column.ColumnName, keyColumn, StringComparison.OrdinalIgnoreCase))
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

        internal static List<Dictionary<string, object>> Matching(DataTable table, string keyword)
        {
            string filter = (keyword ?? string.Empty).Trim();
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            foreach (DataRow row in table.Rows)
            {
                if (filter.Length > 0 && !RowContains(row, filter))
                {
                    continue;
                }

                rows.Add(RowMap(row));
            }

            return rows;
        }

        private static bool RowContains(DataRow row, string filter)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                string text = Convert.ToString(row[column], CultureInfo.InvariantCulture);

                if (text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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

        internal static string QueryReply(string name, List<Dictionary<string, object>> rows)
        {
            string json = new JavaScriptSerializer().Serialize(rows);

            return name + ServerMessageFormat.ReplyNameSuffix
                    + " " + ServerMessageFormat.FieldReturnCode + "=[" + ServerMessageFormat.SuccessReturnCode + "]"
                    + " " + ServerMessageFormat.FieldResultMessage + "=" + json;
        }

        internal static string ActionReply(string name, string message)
        {
            return ActionReply(name, message, null, null);
        }

        internal static string ActionReply(string name, string message, string keyColumn, string returnedKey)
        {
            string reply = name + ServerMessageFormat.ReplyNameSuffix
                    + " " + ServerMessageFormat.FieldReturnCode + "=[" + ServerMessageFormat.SuccessReturnCode + "]"
                    + " " + ServerMessageFormat.FieldSendMessage + "=[" + Clean(message) + "]";

            if (string.IsNullOrEmpty(keyColumn) || string.IsNullOrEmpty(returnedKey))
            {
                return reply;
            }

            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            Dictionary<string, object> row = new Dictionary<string, object>();
            row[keyColumn] = returnedKey;
            rows.Add(row);

            return reply + " " + ServerMessageFormat.FieldResultMessage + "=" + new JavaScriptSerializer().Serialize(rows);
        }

        internal static string Rejected(string name, string code, string message)
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
    }
}
