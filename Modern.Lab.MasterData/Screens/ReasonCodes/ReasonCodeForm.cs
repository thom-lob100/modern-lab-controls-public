using System;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;

using Modern.Lab.Hosting.MasterData;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;

namespace Modern.Lab.MasterData
{
    public partial class ReasonCodeForm : MasterDataCrudFormBase
    {
        private int selectedIndex = -1;
        private bool bindingSelection;

        public ReasonCodeForm()
        {
            this.InitializeComponent();
            this.InitializeCrud(CreateDefinition());
            this.DefineEditors();
        }

        private new void OnSearchClick(object sender, EventArgs e)
        {
            if (!this.ActionInProgress) { base.OnSearchClick(sender, e); }
        }

        private new void OnKeywordEnterPressed(object sender, EventArgs e)
        {
            if (!this.ActionInProgress) { base.OnKeywordEnterPressed(sender, e); }
        }

        private new async void OnSaveClick(object sender, EventArgs e)
        {
            base.OnSaveClick(sender, e);
            await this.GuardPendingWriteAsync();
        }

        private new async void OnDeleteClick(object sender, EventArgs e)
        {
            base.OnDeleteClick(sender, e);
            await this.GuardPendingWriteAsync();
        }

        protected override void OnActionMenuOpening(object sender, CancelEventArgs e)
        {
            base.OnActionMenuOpening(sender, e);
            if (this.ActionInProgress) { this.LockWriteInputs(); }
        }

        private void LockWriteInputs()
        {
            this.RefreshCrudActionState();
            this.txtKeyword.Enabled = false;
            this.btnSearch.Enabled = false;
            this.gridReasonCodes.Enabled = false;
            this.CrudEditor.Enabled = false;
        }

        private async Task GuardPendingWriteAsync()
        {
            if (!this.ActionInProgress) { return; }
            this.LockWriteInputs();
            while (!this.IsDisposed && !this.Disposing && this.ActionInProgress)
            {
                await Task.Delay(25);
            }
            if (this.IsDisposed || this.Disposing) { return; }
            this.RefreshCrudActionState();
            this.txtKeyword.Enabled = true;
            this.btnSearch.Enabled = true;
            this.gridReasonCodes.Enabled = true;
        }

        private static MasterDataCrudDefinition CreateDefinition()
        {
            return new MasterDataCrudDefinition
            {
                EntityName = "Reason Code",
                KeyColumns = KeyColumns,
                ListTableId = "ReasonCode.SelectReasonCodes.ReasonCode"
            };
        }

        protected override void OnListLoading()
        {
            this.gridReasonCodes.DataSource = null;
            this.CrudEditor.SetSchema(new DataTable());
            this.CrudEditor.BeginNew();
        }

        private DataActionResult PrepareSavedItemReload(DataActionResult reply)
        {
            if (reply.Success && !this.IsDisposed && this.IsHandleCreated)
            {
                this.Invoke(new Action(() => this.txtKeyword.Text = string.Empty));
            }
            return reply;
        }

        protected override void OnLoadFailed(string channel, Exception failure)
        {
            if (failure is ResponseContractException && !this.LoadFailureSilent)
            {
                this.ShowErrorMessage(this.QueryFailedCaption, ResponseColumns.MissingMessage, failure.Message);
                return;
            }
            base.OnLoadFailed(channel, failure);
        }

        private void OnReasonCodeSelectionChanged(object sender, EventArgs e)
        {
            if (this.bindingSelection) { return; }
            if (this.ActionInProgress)
            {
                this.bindingSelection = true;
                this.gridReasonCodes.SelectedIndex = this.selectedIndex;
                this.bindingSelection = false;
                return;
            }
            this.selectedIndex = this.gridReasonCodes.SelectedIndex;
            this.HandleSelectionChanged();
        }
        protected override DataTable RequestItems(string keyword)
        {
            DataTable table = this.RequestReasonCodes(keyword);
            string missing = ResponseColumns.Missing(table, KeyColumns);
            if (missing.Length > 0)
            {
                throw new ResponseContractException("Reason Code — missing columns: " + missing);
            }
            return table;
        }
        protected override DataActionResult InsertItem(object[] requestFields)
        {
            return this.PrepareSavedItemReload(this.WriteReasonCode("InsertReasonCode", KeyOf(requestFields, true), requestFields));
        }
        protected override DataActionResult UpdateItem(object[] requestFields)
        {
            return this.PrepareSavedItemReload(this.WriteReasonCode("UpdateReasonCode", KeyOf(requestFields, false), requestFields));
        }
        protected override DataActionResult DeleteItem(string[] keyValues)
        {
            return this.WriteReasonCode("DeleteReasonCode", keyValues, new object[0]);
        }
    }
}
