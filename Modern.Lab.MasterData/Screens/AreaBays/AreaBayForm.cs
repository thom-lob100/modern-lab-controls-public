using System;
using System.Data;

namespace Modern.Lab.MasterData
{
    public partial class AreaBayForm : MasterDataCrudFormBase
    {
        private static readonly string[] RequiredColumns =
        {
            "LOCATION_TYPE", "LOCATION_NAME", "FAB_ID", "PARENT_AREA_ID", "USE_YN"
        };

        public AreaBayForm()
        {
            this.InitializeComponent();
            this.InitializeCrud(CreateDefinition(), this.CreateView());
            DataTable types = new DataTable();
            types.Columns.Add("CODE");
            types.Columns.Add("NAME");
            types.Rows.Add("AREA", "Area");
            types.Rows.Add("BAY", "Bay");
            this.CrudEditor.DefineCombo("LOCATION_TYPE", types, "CODE", "NAME");
            this.CrudEditor.DefineCombo("PARENT_AREA_ID", "SelectParentAreas", "CODE", "NAME");
            this.CrudEditor.DefineToggle("USE_YN");
            this.CrudEditor.DefineMultiline("DESCRIPTION");
            this.CrudEditor.DefineRequired("LOCATION_TYPE", "LOCATION_NAME", "FAB_ID");
            this.CrudEditor.DefineReadOnly("UPDATED_BY", "UPDATED_AT");
            this.RefreshCrudActionState();
        }

        private static MasterDataCrudDefinition CreateDefinition()
        {
            return new MasterDataCrudDefinition
            {
                EntityName = "Area / Bay",
                KeyColumn = "LOCATION_ID",
                ActionName = "AreaBayAction",
                ListTableId = "AreaBay.SelectLocations.Location",
                SelectCommand = "SelectLocations",
                InsertCommand = "InsertLocation",
                UpdateCommand = "UpdateLocation",
                DeleteCommand = "DeleteLocation",
                RequiredEditorColumns = RequiredColumns
            };
        }

        private MasterDataCrudView CreateView()
        {
            return new MasterDataCrudView
            {
                Keyword = this.txtKeyword,
                Grid = this.gridLocations,
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

        protected override void OnListLoading()
        {
            this.gridLocations.DataSource = null;
            this.CrudEditor.SetSchema(new DataTable());
            this.CrudEditor.BeginNew();
            this.LoadAsync("area-lookups",
                    () => this.CrudEditor.FetchComboSources(this.RequestLookupItems),
                    this.CrudEditor.ApplyComboSources);
        }

        private void OnLocationSelectionChanged(object sender, EventArgs e)
        {
            this.HandleSelectionChanged();
        }
    }
}
