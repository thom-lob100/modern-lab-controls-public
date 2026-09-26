using System;
using System.Data;

using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.WinForms.Controls.Selection;

using Modern.Lab.Hosting.MasterData;

namespace Modern.Lab.MasterData
{
    public partial class ProductForm : MasterDataCrudFormBase
    {
        private const string ChannelCombos = "combos";
        private const string ChannelSearchLotCodes = "searchLotCodes";
        private const string ChoiceValueColumn = "VALUE";
        private const string ChoiceLabelColumn = "LABEL";
        private const string LotCodeRequest = "GetLotCodeList";
        private const string LotCodeColumn = "LOT_CD";
        private const string LotNameColumn = "LOT_NM";
        private const string AllChoiceText = "All";

        private static readonly string[] RequiredColumns = new string[] { "PRODUCT_TYPE" };

        private string searchSubProdTyp = string.Empty;
        private string searchLotCd = string.Empty;
        private string searchProdId = string.Empty;

        public ProductForm()
        {
            this.InitializeComponent();
            this.InitializeCrud(CreateDefinition());
            this.DefineEditors();
            this.InitializeSearchChoices();
            this.RefreshCrudActionState();
        }

        private static MasterDataCrudDefinition CreateDefinition()
        {
            return new MasterDataCrudDefinition
            {
                EntityName = "Product",
                KeyColumn = "PROD_ID",
                ListTableId = "Product.SelectProducts.Product",
                RequiredEditorColumns = RequiredColumns
            };
        }

        protected override void OnActionMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            base.OnActionMenuOpening(sender, e);
            this.miDuplicate.Enabled = this.DuplicateSourceKey().Length > 0;
        }

        private string DuplicateSourceKey()
        {
            return this.CrudEditor.IsNew ? string.Empty : this.CrudEditor.ReadKey().Trim();
        }

        private void OnDuplicateClick(object sender, EventArgs e)
        {
            string key = this.DuplicateSourceKey();

            if (key.Length == 0)
            {
                return;
            }

            if (!this.Confirm("Duplicate product '" + key + "'?", "Duplicate Product"))
            {
                return;
            }

            this.RunAction(
                    () => this.DuplicateProduct(key),
                    reply => this.AfterDuplicated(reply),
                    "Duplicating product…");
        }

        private void AfterDuplicated(DataActionResult reply)
        {
            this.ShowToast(reply.Message.Length > 0 ? reply.Message : "Duplicated.");
            this.ReloadItems();
        }

        protected override void OnListLoading()
        {
            this.searchSubProdTyp = SelectedText(this.cboSubProdTyp);
            this.searchLotCd = SelectedText(this.cboLotCd);
            this.searchProdId = this.txtProdId.Text.Trim();
        }

        protected override string ListQueryCriteria(string keyword)
        {
            return Modern.Lab.Hosting.NewItems.NewItemTracker.Criteria(this.searchSubProdTyp, this.searchLotCd, this.searchProdId);
        }

        protected override void LoadLookupSources()
        {
            this.LoadAsync(
                    ChannelCombos,
                    () => this.CrudEditor.FetchComboSources(this.RequestLookupItems),
                    this.CrudEditor.ApplyComboSources);
            this.LoadAsync(
                    ChannelSearchLotCodes,
                    () => this.RequestLookupItems(LotCodeRequest, null),
                    this.ApplyLotCodeChoices);
        }

        private void InitializeSearchChoices()
        {
            this.cboSubProdTyp.DisplayMember = ChoiceLabelColumn;
            this.cboSubProdTyp.ValueMember = ChoiceValueColumn;
            this.cboSubProdTyp.DataSource = WithAllChoice(SubProdTypChoices(), ChoiceValueColumn, ChoiceLabelColumn);
            this.cboSubProdTyp.SelectedIndex = 0;

            this.cboLotCd.DisplayMember = LotNameColumn;
            this.cboLotCd.ValueMember = LotCodeColumn;
            this.ApplyLotCodeChoices(null);
        }

        private void ApplyLotCodeChoices(DataTable lotCodes)
        {
            DataTable choices = new DataTable();
            choices.Columns.Add(LotCodeColumn, typeof(string));
            choices.Columns.Add(LotNameColumn, typeof(string));

            if (lotCodes != null && lotCodes.Columns.Contains(LotCodeColumn))
            {
                foreach (DataRow row in lotCodes.Rows)
                {
                    string code = Convert.ToString(row[LotCodeColumn]).Trim();
                    string name = lotCodes.Columns.Contains(LotNameColumn) ? Convert.ToString(row[LotNameColumn]).Trim() : string.Empty;

                    if (code.Length > 0)
                    {
                        choices.Rows.Add(code, name.Length > 0 ? code + " — " + name : code);
                    }
                }
            }

            string keep = SelectedText(this.cboLotCd);
            this.cboLotCd.DataSource = WithAllChoice(choices, LotCodeColumn, LotNameColumn);
            this.cboLotCd.SelectedValue = keep;

            if (this.cboLotCd.SelectedIndex < 0)
            {
                this.cboLotCd.SelectedIndex = 0;
            }
        }

        private static DataTable SubProdTypChoices()
        {
            DataTable choices = new DataTable();
            choices.Columns.Add(ChoiceValueColumn, typeof(string));
            choices.Columns.Add(ChoiceLabelColumn, typeof(string));
            choices.Rows.Add(ServerFields.Lot.SubProdTyp.Wafer, ServerFields.Lot.SubProdTyp.Wafer);
            choices.Rows.Add(ServerFields.Lot.SubProdTyp.Chip, ServerFields.Lot.SubProdTyp.Chip);
            choices.Rows.Add(ServerFields.Lot.SubProdTyp.Lamella, ServerFields.Lot.SubProdTyp.Lamella);
            return choices;
        }

        private static DataTable WithAllChoice(DataTable choices, string valueColumn, string labelColumn)
        {
            DataTable result = choices.Clone();
            DataRow all = result.NewRow();
            all[valueColumn] = string.Empty;
            all[labelColumn] = AllChoiceText;
            result.Rows.Add(all);

            foreach (DataRow row in choices.Rows)
            {
                result.ImportRow(row);
            }

            return result;
        }

        private static string SelectedText(ModernComboBox combo)
        {
            object value = combo.SelectedValue;
            return value == null ? string.Empty : Convert.ToString(value).Trim();
        }

        private void LoadProducts()
        {
            this.ReloadItems();
        }

        private void OnProductSelectionChanged(object sender, EventArgs e)
        {
            this.HandleSelectionChanged();
        }
    }
}
