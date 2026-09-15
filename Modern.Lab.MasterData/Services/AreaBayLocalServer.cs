using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web.Script.Serialization;

using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.MasterData.Services
{
    internal static class AreaBayLocalServer
    {
        private static readonly object Gate = new object();
        private static readonly string[] EditableColumns =
        {
            "LOCATION_TYPE", "LOCATION_NAME", "FAB_ID", "PARENT_AREA_ID", "USE_YN", "DESCRIPTION"
        };
        private static readonly DataTable Locations = Seed();

        internal static string Send(string request)
        {
            Dictionary<string, object> fields = ReplyMessageParser.Parse(request);
            string name = MasterDataLocalReply.Text(fields, ReplyMessageParser.KeyReplyName);
            string method = MasterDataLocalReply.Text(fields, "MethodCommand");
            lock (Gate)
            {
                if (name != "AreaBayAction")
                {
                    return MasterDataLocalReply.Rejected(name, "UNKNOWN_REQUEST", "Unknown action.");
                }
                switch (method)
                {
                    case "SelectLocations":
                        return Query(method, Locations, MasterDataLocalReply.Text(fields, "Keyword"));
                    case "SelectParentAreas":
                        return Query(method, ParentAreas(), string.Empty);
                    case "InsertLocation":
                        return Save(method, fields, true);
                    case "UpdateLocation":
                        return Save(method, fields, false);
                    case "DeleteLocation":
                        return Delete(method, fields);
                    default:
                        return MasterDataLocalReply.Rejected(method, "UNKNOWN_REQUEST", "Unknown method.");
                }
            }
        }

        internal static DataTable Snapshot()
        {
            lock (Gate)
            {
                return Locations.Copy();
            }
        }

        internal static DataTable ParseTable(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                return ServerMessageFormat.ParseQueryTable(json);
            }

            Dictionary<string, object> envelope = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            object columnsValue;
            object rowsValue;
            if (envelope == null || !envelope.TryGetValue("Columns", out columnsValue)
                    || !envelope.TryGetValue("Rows", out rowsValue))
            {
                throw new FormatException("Missing table schema or rows.");
            }
            object[] columns = columnsValue as object[];
            object[] rows = rowsValue as object[];
            if (columns == null || columns.Length == 0 || rows == null)
            {
                throw new FormatException("Invalid table schema or rows.");
            }
            DataTable table = new DataTable();
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (object value in columns)
            {
                string column = value as string;
                if (string.IsNullOrWhiteSpace(column) || !names.Add(column))
                {
                    throw new FormatException("Invalid or duplicate column.");
                }
                table.Columns.Add(column, typeof(string));
            }
            foreach (object value in rows)
            {
                Dictionary<string, object> source = value as Dictionary<string, object>;
                if (source == null || source.Count != columns.Length)
                {
                    throw new FormatException("Row does not match schema.");
                }
                DataRow row = table.NewRow();
                foreach (DataColumn column in table.Columns)
                {
                    object cell;
                    if (!source.TryGetValue(column.ColumnName, out cell) || (cell != null && !(cell is string)))
                    {
                        throw new FormatException("Cell does not match schema.");
                    }
                    row[column] = cell ?? string.Empty;
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private static string Query(string method, DataTable table, string keyword)
        {
            List<string> columns = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columns.Add(column.ColumnName);
            }
            Dictionary<string, object> envelope = new Dictionary<string, object>();
            envelope.Add("Columns", columns);
            envelope.Add("Rows", MasterDataLocalReply.Matching(table, keyword));
            return method + ServerMessageFormat.ReplyNameSuffix + " ReturnCode=[0] ResultMessage="
                    + new JavaScriptSerializer().Serialize(envelope);
        }

        private static string Save(string method, Dictionary<string, object> fields, bool insert)
        {
            string key = MasterDataLocalReply.Text(fields, "LocationId").Trim();
            DataRow current = Find(key);
            if (key.Length == 0 || (insert && current != null) || (!insert && current == null))
            {
                return MasterDataLocalReply.Rejected(method, "INVALID_KEY", "Code is required, must be unique on insert and must exist on update.");
            }

            DataRow candidate = Locations.NewRow();
            if (current != null)
            {
                candidate.ItemArray = current.ItemArray;
            }
            candidate["LOCATION_ID"] = current == null ? key : current["LOCATION_ID"];
            foreach (string column in EditableColumns)
            {
                object value;
                if (fields.TryGetValue(MasterDataLocalReply.Param(column), out value))
                {
                    candidate[column] = (Convert.ToString(value) ?? string.Empty).Trim();
                }
            }
            candidate["LOCATION_TYPE"] = Value(candidate, "LOCATION_TYPE").ToUpperInvariant();
            candidate["USE_YN"] = Value(candidate, "USE_YN").ToUpperInvariant();
            string error = Validate(candidate);
            if (error != null)
            {
                return MasterDataLocalReply.Rejected(method, "INVALID_LOCATION", error);
            }

            candidate["UPDATED_BY"] = Environment.UserName;
            candidate["UPDATED_AT"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            if (insert)
            {
                Locations.Rows.Add(candidate);
            }
            else
            {
                current.ItemArray = candidate.ItemArray;
            }
            return MasterDataLocalReply.ActionReply(method, "Location saved.", "LOCATION_ID", Value(candidate, "LOCATION_ID"));
        }

        private static string Validate(DataRow candidate)
        {
            string key = Value(candidate, "LOCATION_ID");
            string type = Value(candidate, "LOCATION_TYPE");
            string fab = Value(candidate, "FAB_ID");
            string parentKey = Value(candidate, "PARENT_AREA_ID");
            string enabled = Value(candidate, "USE_YN");
            if ((type != "AREA" && type != "BAY") || Value(candidate, "LOCATION_NAME").Length == 0
                    || fab.Length == 0 || (enabled != "Y" && enabled != "N"))
            {
                return "Type (AREA or BAY), name, Fab and enabled (Y or N) are required.";
            }
            if (type == "AREA" && parentKey.Length != 0)
            {
                return "An Area cannot have a parent. Clear Parent Area Id first.";
            }
            if (type == "BAY")
            {
                DataRow parent = Find(parentKey);
                if (parent == null || Equal(key, parentKey) || Value(parent, "LOCATION_TYPE") != "AREA"
                        || !Equal(fab, Value(parent, "FAB_ID")))
                {
                    return "A Bay requires a parent Area in the same Fab.";
                }
                if (enabled == "Y" && Value(parent, "USE_YN") != "Y")
                {
                    return "An enabled Bay requires an enabled parent Area.";
                }
                candidate["PARENT_AREA_ID"] = parent["LOCATION_ID"];
            }
            foreach (DataRow child in Locations.Rows)
            {
                if (!Equal(Value(child, "PARENT_AREA_ID"), key))
                {
                    continue;
                }
                if (type != "AREA" || !Equal(Value(child, "FAB_ID"), fab))
                {
                    return "Move or delete child Bays before changing the Area type or Fab.";
                }
                if (enabled != "Y" && Value(child, "USE_YN") == "Y")
                {
                    return "Disable child Bays before disabling their Area.";
                }
            }
            return null;
        }

        private static string Delete(string method, Dictionary<string, object> fields)
        {
            string key = MasterDataLocalReply.Text(fields, "LocationId").Trim();
            DataRow current = Find(key);
            if (current == null)
            {
                return MasterDataLocalReply.Rejected(method, "NOT_FOUND", "Location was not found.");
            }
            foreach (DataRow child in Locations.Rows)
            {
                if (Equal(Value(child, "PARENT_AREA_ID"), key))
                {
                    return MasterDataLocalReply.Rejected(method, "HAS_CHILDREN", "Move or delete child Bays before deleting their Area.");
                }
            }
            Locations.Rows.Remove(current);
            return MasterDataLocalReply.ActionReply(method, "Location deleted.");
        }

        private static DataRow Find(string key)
        {
            foreach (DataRow row in Locations.Rows)
            {
                if (Equal(Value(row, "LOCATION_ID"), key))
                {
                    return row;
                }
            }
            return null;
        }

        private static DataTable ParentAreas()
        {
            DataTable table = new DataTable();
            table.Columns.Add("CODE");
            table.Columns.Add("NAME");
            table.Rows.Add(string.Empty, "None (Area only)");
            foreach (DataRow row in Locations.Rows)
            {
                if (Value(row, "LOCATION_TYPE") == "AREA")
                {
                    table.Rows.Add(row["LOCATION_ID"], Value(row, "LOCATION_ID") + " - " + Value(row, "LOCATION_NAME")
                            + " / " + Value(row, "FAB_ID") + (Value(row, "USE_YN") == "Y" ? string.Empty : " (inactive)"));
                }
            }
            return table;
        }

        private static string Value(DataRow row, string column)
        {
            return Convert.ToString(row[column], CultureInfo.InvariantCulture);
        }

        private static bool Equal(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static DataTable Seed()
        {
            DataTable table = new DataTable("AREA_BAY");
            table.Columns.Add("LOCATION_ID");
            foreach (string column in EditableColumns)
            {
                table.Columns.Add(column);
            }
            table.Columns.Add("UPDATED_BY");
            table.Columns.Add("UPDATED_AT");
            table.Rows.Add("AREA-ETCH", "AREA", "Etch", "FAB-01", "", "Y", "Etch process area", "demo", "");
            table.Rows.Add("AREA-PHOTO", "AREA", "Lithography", "FAB-01", "", "Y", "Lithography process area", "demo", "");
            table.Rows.Add("BAY-ETCH-01", "BAY", "Etch Bay 01", "FAB-01", "AREA-ETCH", "Y", "Etch equipment location", "demo", "");
            table.Rows.Add("BAY-PHOTO-01", "BAY", "Lithography Bay 01", "FAB-01", "AREA-PHOTO", "N", "Reserved bay", "demo", "");
            return table;
        }
    }
}
