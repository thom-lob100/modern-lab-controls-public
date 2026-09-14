using System.Data;
using System.Drawing;
using Modern.Lab.Data;
using Modern.Lab.Theming;

namespace Modern.Lab.Samples.Services
{
    internal static class NewItemTint
    {
        internal const string ColumnName = "NewItemTint";

        private const double SurfaceRatio = 0.16;

        internal static string Member(DataTable table)
        {
            return table != null && table.Columns.Contains(ColumnName) ? ColumnName : string.Empty;
        }

        internal static DataTable Apply(DataTable table, bool judged)
        {
            if (!judged || table == null || table.Columns.Contains(ColumnName)
                    || !table.Columns.Contains(NewItemTracker.ColumnName))
            {
                return table;
            }

            string tint = RowTint();
            table.Columns.Add(ColumnName, typeof(string));

            foreach (DataRow row in table.Rows)
            {
                row[ColumnName] = TableHelper.CellText(row, NewItemTracker.ColumnName) == NewItemTracker.MarkText
                        ? tint
                        : string.Empty;
            }

            return table;
        }

        private static string RowTint()
        {
            Color surface = ModernTokenColors.Get("Brush.Surface", Color.White);
            Color mark = ModernTokenColors.Get("Brush.Success", Color.FromArgb(22, 163, 74));
            return ColorTranslator.ToHtml(ModernTokenColors.Blend(mark, surface, SurfaceRatio));
        }
    }
}
