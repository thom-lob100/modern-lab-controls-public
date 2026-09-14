using System;

namespace Modern.Lab.MasterData
{
    public partial class ProductForm : MasterDataCrudFormBase
    {
        private const string ChannelCombos = "combos";

        private static readonly string[] RequiredColumns = new string[] { "PRODUCT_NAME", "PRODUCT_TYPE" };

        public ProductForm()
        {
            this.InitializeComponent();
            this.InitializeCrud(CreateDefinition(), this.CreateView());
            this.DefineEditors();
            this.RefreshCrudActionState();
        }

        private static MasterDataCrudDefinition CreateDefinition()
        {
            return new MasterDataCrudDefinition
            {
                EntityName = "Product",
                KeyColumn = "PROD_ID",
                ActionName = "ProductAction",
                ListTableId = "Product.SelectProducts.Product",
                SelectCommand = "SelectProducts",
                InsertCommand = "InsertProduct",
                UpdateCommand = "UpdateProduct",
                DeleteCommand = "DeleteProduct",
                RequiredEditorColumns = RequiredColumns
            };
        }

        private MasterDataCrudView CreateView()
        {
            return new MasterDataCrudView
            {
                Keyword = this.txtKeyword,
                Grid = this.gridProducts,
                EditorCard = this.editorCard,
                Editor = this.propertyGrid,
                NewButton = this.btnNew,
                CancelButton = this.btnCancel,
                SaveButton = this.btnSave,
                DeleteButton = this.btnDelete,
                ActionMenu = this.menuActions,
                NewMenuItem = this.miNew,
                CancelMenuItem = this.miCancel,
                SaveMenuItem = this.miSave,
                DeleteMenuItem = this.miDelete
            };
        }

        private void DefineEditors()
        {
            this.CrudEditor.DefineCombo("PRODUCT_TYPE", "GetProductTypeList", "CODE", "NAME");
            this.CrudEditor.DefineCombo("UNIT", "GetCodeList", "CODE", "NAME", "GroupCd", "UNIT");
            this.CrudEditor.DefineToggle("USE_YN");
            this.CrudEditor.DefineMultiline("DESCRIPTION");
            this.CrudEditor.DefineRequired(RequiredColumns);
            this.CrudEditor.DefineReadOnly("PROD_ID", "UPDATED_BY", "UPDATED_AT");
        }

        protected override void LoadLookupSources()
        {
            this.LoadAsync(
                    ChannelCombos,
                    () => this.CrudEditor.FetchComboSources(this.RequestLookupItems),
                    this.CrudEditor.ApplyComboSources);
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
