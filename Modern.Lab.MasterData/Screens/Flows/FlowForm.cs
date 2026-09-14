using System;

namespace Modern.Lab.MasterData
{
    public partial class FlowForm : MasterDataCrudFormBase
    {
        public FlowForm()
        {
            this.InitializeComponent();
            this.InitializeCrud(CreateDefinition(), this.CreateView());
        }

        private static MasterDataCrudDefinition CreateDefinition()
        {
            return new MasterDataCrudDefinition
            {
                EntityName = "Flow",
                KeyColumn = "FLOW_ID",
                ActionName = "FlowAction",
                ListTableId = "Flow.SelectFlows.Flow",
                SelectCommand = "SelectFlows",
                InsertCommand = "InsertFlow",
                UpdateCommand = "UpdateFlow",
                DeleteCommand = "DeleteFlow"
            };
        }

        private MasterDataCrudView CreateView()
        {
            return new MasterDataCrudView
            {
                Keyword = this.txtKeyword,
                Grid = this.gridFlows,
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

        private void LoadFlows()
        {
            this.ReloadItems();
        }

        private void OnFlowSelectionChanged(object sender, EventArgs e)
        {
            this.HandleSelectionChanged();
        }
    }
}
