using System;
using System.Data;

using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting.MasterData;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;

namespace Modern.Lab.MasterData
{
    public partial class PortForm : MasterDataCrudFormBase
    {
        private string queryEqpId = string.Empty;
        private string actionEqpId = string.Empty;
        private bool bindingEquipment;
        private bool loadingEquipment;

        public PortForm()
        {
            this.InitializeComponent();
            this.InitializeCrud(CreateDefinition());
            this.DefineEditors();
        }

        private static MasterDataCrudDefinition CreateDefinition()
        {
            return new MasterDataCrudDefinition
            {
                EntityName = "Port",
                KeyColumn = "PORT_ID",
                ListTableId = "Port.SelectPorts.Port",
                RequiredEditorColumns = new[] { ServerFields.Port.PortTyp.Column }
            };
        }

        private string SelectedEqpId
        {
            get { return Convert.ToString(this.cmbEquipment.SelectedValue).Trim(); }
        }

        protected override void OnFormLoad(object sender, EventArgs e)
        {
            this.LoadEquipment();
        }

        private void LoadEquipment()
        {
            if (this.loadingEquipment || this.ActionInProgress)
            {
                return;
            }
            this.loadingEquipment = true;
            this.cmbEquipment.Enabled = false;
            this.btnSearch.Enabled = false;
            this.gridPorts.EmptyText = "Loading equipment...";
            bool applied = false;
            this.LoadAsync("equipment",
                    this.RequestEquipment,
                    table =>
                    {
                        string missing = ResponseColumns.Missing(table, ServerFields.Equipment.EqpId);
                        if (missing.Length > 0)
                        {
                            this.ShowErrorMessage(this.QueryFailedCaption, ResponseColumns.MissingMessage,
                                    "Equipment — missing column " + missing);
                            return;
                        }
                        this.bindingEquipment = true;
                        try
                        {
                            this.cmbEquipment.DataSource = table;
                            this.cmbEquipment.SelectedIndex = table.Rows.Count > 0 ? 0 : -1;
                        }
                        finally
                        {
                            this.bindingEquipment = false;
                        }
                        applied = true;
                        this.cmbEquipment.Enabled = table.Rows.Count > 0;
                        this.gridPorts.EmptyText = "No equipment available";
                        if (this.SelectedEqpId.Length > 0)
                        {
                            this.ReloadItems();
                        }
                    },
                    current =>
                    {
                        if (current)
                        {
                            this.loadingEquipment = false;
                            this.btnSearch.Enabled = true;
                            if (!applied)
                            {
                                this.gridPorts.EmptyText = "Equipment could not be loaded";
                            }
                        }
                    });
        }

        private void OnEquipmentChanged(object sender, EventArgs e)
        {
            if (this.bindingEquipment)
            {
                return;
            }
            if (this.ActionInProgress || this.SelectedEqpId.Length == 0)
            {
                this.bindingEquipment = true;
                try
                {
                    this.cmbEquipment.SelectedValue = this.queryEqpId;
                }
                finally
                {
                    this.bindingEquipment = false;
                }
                return;
            }
            if (this.SelectedEqpId != this.queryEqpId)
            {
                this.ReloadItems();
            }
        }

        protected override void OnSearchClick(object sender, EventArgs e)
        {
            this.SearchPorts();
        }

        protected override void OnKeywordEnterPressed(object sender, EventArgs e)
        {
            this.SearchPorts();
        }

        private void SearchPorts()
        {
            if (this.ActionInProgress || this.loadingEquipment)
            {
                return;
            }
            if (this.SelectedEqpId.Length == 0)
            {
                this.LoadEquipment();
                return;
            }
            this.ReloadItems();
        }

        protected override void OnListLoading()
        {
            this.queryEqpId = this.SelectedEqpId;
            this.gridPorts.DataSource = null;
            this.gridPorts.EmptyText = "No ports";
            this.CrudEditor.SetSchema(new DataTable());
            this.CrudEditor.BeginNew();
        }

        protected override bool CanStartAction()
        {
            if (this.SelectedEqpId.Length == 0 || this.SelectedEqpId != this.queryEqpId
                    || !base.CanStartAction())
            {
                return false;
            }
            this.actionEqpId = this.queryEqpId;
            return true;
        }

        private void OnPortSelectionChanged(object sender, EventArgs e)
        {
            this.HandleSelectionChanged();
        }
        protected override DataTable RequestItems(string keyword)
        {
            string eqpId = this.queryEqpId;
            if (eqpId.Length == 0)
            {
                throw new InvalidOperationException("Select equipment before searching for ports.");
            }
            return this.RequestPorts(eqpId, keyword);
        }
        protected override DataActionResult InsertItem(object[] requestFields)
        {
            if (this.actionEqpId.Length == 0)
            {
                throw new InvalidOperationException("Select equipment before editing ports.");
            }
            return this.WritePort("InsertPort", this.actionEqpId, requestFields);
        }
        protected override DataActionResult UpdateItem(object[] requestFields)
        {
            if (this.actionEqpId.Length == 0)
            {
                throw new InvalidOperationException("Select equipment before editing ports.");
            }
            return this.WritePort("UpdatePort", this.actionEqpId, requestFields);
        }
        protected override DataActionResult DeleteItem(string key)
        {
            if (this.actionEqpId.Length == 0)
            {
                throw new InvalidOperationException("Select equipment before editing ports.");
            }
            return this.WritePort("DeletePort", this.actionEqpId, new object[] { "PortId", key });
        }
    }
}
