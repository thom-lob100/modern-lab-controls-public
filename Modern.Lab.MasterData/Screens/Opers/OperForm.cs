using System;

namespace Modern.Lab.MasterData
{
    public partial class OperForm : MasterDataCrudFormBase
    {
        public OperForm()
        {
            this.InitializeComponent();
            this.InitializeCrud(CreateDefinition(), this.CreateView());
        }

        private static MasterDataCrudDefinition CreateDefinition()
        {
            return new MasterDataCrudDefinition
            {
                EntityName = "Oper",
                KeyColumn = "OPER_ID",
                ActionName = "OperAction",
                ListTableId = "Oper.SelectOpers.Oper",
                SelectCommand = "SelectOpers",
                InsertCommand = "InsertOper",
                UpdateCommand = "UpdateOper",
                DeleteCommand = "DeleteOper"
            };
        }

        private MasterDataCrudView CreateView()
        {
            return new MasterDataCrudView
            {
                Keyword = this.txtKeyword,
                Grid = this.gridOpers,
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

        private void LoadOpers()
        {
            this.ReloadItems();
        }

        private void OnOperSelectionChanged(object sender, EventArgs e)
        {
            this.HandleSelectionChanged();
        }
    }
}
