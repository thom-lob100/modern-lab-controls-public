using System.Data;

namespace Modern.Lab.Hosting.NewItems
{
    public static class NewItemTint
    {
        public const string ColumnName = "NewItemTint";

        /// <summary>
        /// 보조 컬럼 없이 신규 행에 작은 New를 표시하고, 지정 Id 또는 현재 뷰의 첫 신규 행으로 이동한다.
        /// <paramref name="keyColumn"/>은 <see cref="NewItemTracker.Accept"/>에 준 키와 같아야 한다(복합 키는 쉼표 목록).
        /// </summary>
        public static bool Bind(Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid, NewItemTracker tracker,
                string keyColumn, string createdId = null, bool selectNewItems = true)
        {
            string[] keys = NewItemTracker.KeyColumns(keyColumn);
            grid.RowMarkerSelector = item => tracker.IsNew(ItemId(item, keys)) ? NewItemTracker.MarkText : string.Empty;
            string preferred = (createdId ?? string.Empty).Trim();
            return grid.SelectRow(item => preferred.Length > 0
                    ? ItemId(item, keys) == preferred : selectNewItems && tracker.IsNew(ItemId(item, keys)));
        }

        private static string ItemId(object item, string[] keys)
        {
            DataRowView view = item as DataRowView;
            DataRow row = view == null ? item as DataRow : view.Row;
            return NewItemTracker.RowId(row, keys);
        }

    }
}
