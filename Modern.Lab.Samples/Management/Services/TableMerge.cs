using System;
using System.Collections.Generic;
using System.Data;
using Modern.Lab.Data;

namespace Modern.Lab.Samples.Services
{



    public static class TableMerge
    {
        public static bool Apply(DataTable target, DataTable source, string keyColumn)
        {
            return Apply(target, source, keyColumn, null);
        }

        public static bool Apply(DataTable target, DataTable source, string keyColumn, ICollection<string> keepColumns)
        {
            if (target == null)
            {
                return false;
            }

            bool changed = false;

            if (source == null)
            {
                changed = target.Rows.Count > 0;
                target.Rows.Clear();
                return changed;
            }

            foreach (DataColumn column in source.Columns)
            {
                if (!target.Columns.Contains(column.ColumnName))
                {
                    target.Columns.Add(column.ColumnName, column.DataType);
                    changed = true;
                }
            }

            if (string.IsNullOrEmpty(keyColumn))
            {
                int paired = Math.Min(target.Rows.Count, source.Rows.Count);

                for (int index = 0; index < paired; index++)
                {
                    changed = UpdateRow(target.Rows[index], source.Rows[index], source, keepColumns) || changed;
                }

                for (int index = target.Rows.Count - 1; index >= source.Rows.Count; index--)
                {
                    target.Rows.RemoveAt(index);
                    changed = true;
                }

                for (int index = target.Rows.Count; index < source.Rows.Count; index++)
                {
                    DataRow added = target.NewRow();
                    CopyRow(added, source.Rows[index], source);
                    target.Rows.Add(added);
                    changed = true;
                }

                return changed;
            }

            if (!Keyed(target, keyColumn) || !Keyed(source, keyColumn))
            {
                changed = changed || target.Rows.Count > 0 || source.Rows.Count > 0;
                target.Rows.Clear();

                foreach (DataRow incoming in source.Rows)
                {
                    DataRow added = target.NewRow();
                    CopyRow(added, incoming, source);
                    target.Rows.Add(added);
                }

                return changed;
            }

            Dictionary<string, DataRow> existing = new Dictionary<string, DataRow>(StringComparer.Ordinal);

            foreach (DataRow row in target.Rows)
            {
                string key = TableHelper.CellText(row, keyColumn).Trim();

                if (key.Length > 0 && !existing.ContainsKey(key))
                {
                    existing.Add(key, row);
                }
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (DataRow incoming in source.Rows)
            {
                string key = TableHelper.CellText(incoming, keyColumn).Trim();
                seen.Add(key);

                DataRow current;

                if (existing.TryGetValue(key, out current))
                {
                    changed = UpdateRow(current, incoming, source, keepColumns) || changed;
                }
                else
                {
                    DataRow added = target.NewRow();
                    CopyRow(added, incoming, source);
                    target.Rows.Add(added);
                    changed = true;
                }
            }

            List<DataRow> removed = new List<DataRow>();

            foreach (DataRow row in target.Rows)
            {
                if (!seen.Contains(TableHelper.CellText(row, keyColumn).Trim()))
                {
                    removed.Add(row);
                }
            }

            foreach (DataRow row in removed)
            {
                target.Rows.Remove(row);
                changed = true;
            }

            return changed;
        }

        private static bool Keyed(DataTable table, string keyColumn)
        {
            if (string.IsNullOrEmpty(keyColumn) || !table.Columns.Contains(keyColumn))
            {
                return false;
            }

            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);

            foreach (DataRow row in table.Rows)
            {
                string key = TableHelper.CellText(row, keyColumn).Trim();

                if (key.Length == 0 || !keys.Add(key))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool UpdateRow(DataRow current, DataRow incoming, DataTable source, ICollection<string> keepColumns)
        {
            bool changed = false;

            current.BeginEdit();

            foreach (DataColumn column in source.Columns)
            {
                object value = incoming[column];

                if (!SameValue(current[column.ColumnName], value))
                {
                    current[column.ColumnName] = value;
                    changed = true;
                }
            }

            foreach (DataColumn column in current.Table.Columns)
            {
                if (source.Columns.Contains(column.ColumnName)
                        || (keepColumns != null && keepColumns.Contains(column.ColumnName)))
                {
                    continue;
                }

                if (!SameValue(current[column], null))
                {
                    current[column] = DBNull.Value;
                    changed = true;
                }
            }

            current.EndEdit();
            return changed;
        }

        private static void CopyRow(DataRow added, DataRow incoming, DataTable source)
        {
            foreach (DataColumn column in source.Columns)
            {
                added[column.ColumnName] = incoming[column];
            }
        }

        private static bool SameValue(object left, object right)
        {
            bool leftNull = left == null || left == DBNull.Value;
            bool rightNull = right == null || right == DBNull.Value;

            if (leftNull || rightNull)
            {
                return leftNull && rightNull;
            }

            return string.Equals(Convert.ToString(left), Convert.ToString(right), StringComparison.Ordinal);
        }
    }
}
