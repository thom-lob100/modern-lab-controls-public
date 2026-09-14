using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Modern.Lab.Data;

namespace Modern.Lab.Samples.Services
{
    internal sealed class NewItemTracker
    {
        internal const string ColumnName = "NewItem";
        internal const string MarkText = "New";
        private HashSet<string> baseline;
        private string criteria;

        internal bool ActionsReady { get; private set; } = true;

        internal bool HasNewItems { get; private set; }

        internal void BeginQuery()
        {
            this.ActionsReady = false;
        }

        internal DataTable Accept(DataTable source, string key, string queryCriteria, bool comparable = true)
        {
            if (source == null)
            {
                return null;
            }

            this.ActionsReady = true;
            this.HasNewItems = false;
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            if (source.Rows.Count == 0 && source.Columns.Count == 0)
            {
                this.baseline = ids;
                this.criteria = queryCriteria;
                return source.Copy();
            }

            int keyColumns = 0;
            foreach (DataColumn column in source.Columns)
            {
                if (string.Equals(column.ColumnName, ColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    return source.Copy();
                }
                if (string.Equals(column.ColumnName, key, StringComparison.OrdinalIgnoreCase))
                {
                    keyColumns++;
                }
            }

            if (!comparable || keyColumns != 1)
            {
                return source.Copy();
            }

            foreach (DataRow row in source.Rows)
            {
                string id = TableHelper.CellText(row, key).Trim();
                if (id.Length == 0 || !ids.Add(id))
                {
                    return source.Copy();
                }
            }

            bool sameCriteria = this.baseline != null && this.criteria == queryCriteria;
            DataTable display = source.Copy();
            display.Columns.Add(ColumnName, typeof(string));
            foreach (DataRow row in display.Rows)
            {
                bool isNew = sameCriteria && !this.baseline.Contains(TableHelper.CellText(row, key).Trim());
                row[ColumnName] = isNew ? MarkText : string.Empty;
                this.HasNewItems |= isNew;
            }

            this.baseline = ids;
            this.criteria = queryCriteria;
            return display;
        }

        internal static string SelectionSet(string values)
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

        internal static string Criteria(params string[] values)
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
