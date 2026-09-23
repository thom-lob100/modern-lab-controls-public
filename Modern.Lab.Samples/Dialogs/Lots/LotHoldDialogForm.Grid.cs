using System.Data;
using System.Windows.Forms;
using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.WinForms.Controls.Display;
using Modern.Lab.WinForms.Controls.Selection;

namespace Modern.Lab.Samples
{
    public partial class LotHoldDialogForm
    {
        // Lot 정보 표시 순서와 콤보의 코드·명칭 컬럼은 회사에서 이 파일을 변경한다.
        private static FieldDefinitions SourceDefinitions(DataTable lot)
        {
            return FieldDefinitions.Of(lot).Only("LOT_ID", "SUB_PROD_TYP", "LOT_STAT_TYP", "LOT_HOLD_STAT_CD",
                    "PROD_ID", "FLOW_ID", "OPER_ID", "QTY");
        }

        private static void ConfigureCodes(ModernComboBox combo)
        {
            combo.DisplayMember = "REASON_CD";
            combo.ValueMember = "REASON_CD";
            combo.CharacterCasing = CharacterCasing.Upper;
            combo.AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-";
            combo.DropDownStyle = ComboBoxStyle.DropDown;
            combo.AutoCompleteMode = AutoCompleteMode.Suggest;
            combo.AutoCompleteSource = AutoCompleteSource.ListItems;
            combo.ConfigureDropDownColumns(new ModernDataGridColumn("REASON_CD", "Code", 150),
                    new ModernDataGridColumn("CTN_DESC", "Description", 240));
        }

        private static void ConfigureEngineers(ModernComboBox combo)
        {
            combo.DisplayMember = "USER_ID";
            combo.ValueMember = "USER_ID";
            combo.DropDownStyle = ComboBoxStyle.DropDown;
            combo.AutoCompleteMode = AutoCompleteMode.Suggest;
            combo.AutoCompleteSource = AutoCompleteSource.ListItems;
            combo.ConfigureDropDownColumns(new ModernDataGridColumn("USER_ID", "User Id", 150),
                    new ModernDataGridColumn("USER_NM", "Name", 240));
        }
    }
}
