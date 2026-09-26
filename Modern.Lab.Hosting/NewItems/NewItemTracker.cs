using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Modern.Lab.Data;

namespace Modern.Lab.Hosting.NewItems
{
    public sealed class NewItemTracker
    {
        public const string ColumnName = "NewItem";
        public const string MarkText = "New";
        private HashSet<string> baseline;
        private readonly HashSet<string> newIds = new HashSet<string>(StringComparer.Ordinal);
        private string criteria;

        public bool ActionsReady { get; private set; } = true;

        public bool HasNewItems { get; private set; }

        public bool IsNew(string id)
        {
            return this.newIds.Contains((id ?? string.Empty).Trim());
        }

        public void BeginQuery()
        {
            this.ActionsReady = false;
        }

        public DataTable Accept(DataTable source, string key, string queryCriteria, bool comparable = true, bool includeMarkerColumn = false)
        {
            if (source == null)
            {
                return null;
            }

            this.ActionsReady = true;
            this.HasNewItems = false;
            this.newIds.Clear();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            if (source.Rows.Count == 0 && source.Columns.Count == 0)
            {
                this.baseline = ids;
                this.criteria = queryCriteria;
                return source.Copy();
            }

            string[] keys = KeyColumns(key);
            int[] keyColumns = new int[keys.Length];
            foreach (DataColumn column in source.Columns)
            {
                if (includeMarkerColumn && string.Equals(column.ColumnName, ColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    return source.Copy();
                }
                for (int index = 0; index < keys.Length; index++)
                {
                    if (string.Equals(column.ColumnName, keys[index], StringComparison.OrdinalIgnoreCase))
                    {
                        keyColumns[index]++;
                    }
                }
            }

            if (!comparable || keys.Length == 0 || Array.Exists(keyColumns, count => count != 1))
            {
                return source.Copy();
            }

            foreach (DataRow row in source.Rows)
            {
                string id = RowId(row, keys);
                if (id.Length == 0 || !ids.Add(id))
                {
                    return source.Copy();
                }
            }

            bool sameCriteria = this.baseline != null && this.criteria == queryCriteria;
            DataTable display = source.Copy();
            if (includeMarkerColumn) { display.Columns.Add(ColumnName, typeof(string)); }
            foreach (DataRow row in display.Rows)
            {
                string id = RowId(row, keys);
                bool isNew = sameCriteria && !this.baseline.Contains(id);
                if (includeMarkerColumn) { row[ColumnName] = isNew ? MarkText : string.Empty; }
                if (isNew) { this.newIds.Add(id); }
                this.HasNewItems |= isNew;
            }

            this.baseline = ids;
            this.criteria = queryCriteria;
            return display;
        }

        // 키는 컬럼 하나 또는 그리드 RowKeyMember와 같은 쉼표 목록("LOT_ID,TIMEKEY")이다.
        internal static string[] KeyColumns(string key)
        {
            List<string> columns = new List<string>();
            foreach (string part in (key ?? string.Empty).Split(','))
            {
                string column = part.Trim();
                if (column.Length > 0)
                {
                    columns.Add(column);
                }
            }
            return columns.ToArray();
        }

        // 행의 비교 Id. 키 값은 앞뒤 공백을 지우고, 하나라도 비면 비교할 수 없으므로 빈 문자열이다.
        internal static string RowId(DataRow row, string[] keys)
        {
            if (row == null || keys.Length == 0)
            {
                return string.Empty;
            }
            string[] values = new string[keys.Length];
            for (int index = 0; index < keys.Length; index++)
            {
                values[index] = TableHelper.CellText(row, keys[index]).Trim();
                if (values[index].Length == 0)
                {
                    return string.Empty;
                }
            }
            return keys.Length == 1 ? values[0] : string.Join("\u001F", values);
        }

        public static string SelectionSet(string values)
        {
            List<string> items = new List<string>();
            foreach (string value in (values ?? string.Empty).Split(','))
            {
                string item = value.Trim();
                if (item.Length > 0 && !items.Contains(item))
                {
                    items.Add(item);
                }
            }
            items.Sort(StringComparer.Ordinal);
            return string.Join(",", items);
        }

        public static string Criteria(params string[] values)
        {
            StringBuilder result = new StringBuilder();
            foreach (string value in values)
            {
                string text = value ?? string.Empty;
                result.Append(text.Length).Append(':').Append(text);
            }
            return result.ToString();
        }
    }
}
