using System;
using System.Data;

using Modern.Lab.Hosting.MasterData;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;

namespace Modern.Lab.MasterData
{
    public partial class UserForm : MasterDataCrudFormBase
    {
        public UserForm()
        {
            this.InitializeComponent();
            this.InitializeCrud(CreateDefinition());
            this.DefineEditors();
        }

        private static MasterDataCrudDefinition CreateDefinition()
        {
            return new MasterDataCrudDefinition
            {
                EntityName = "User",
                KeyColumn = "USER_ID",
                ListTableId = "User.SelectUsers.User"
            };
        }

        protected override void OnListLoading()
        {
            this.gridUsers.DataSource = null;
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

        private void OnUserSelectionChanged(object sender, EventArgs e)
        {
            this.HandleSelectionChanged();
        }
        protected override DataTable RequestItems(string keyword)
        {
            DataTable table = this.RequestUsers(keyword);
            string missing = ResponseColumns.Missing(table, UserIdColumn);
            if (missing.Length > 0)
            {
                throw new ResponseContractException("User — missing column " + missing);
            }
            return table;
        }
        protected override DataActionResult InsertItem(object[] requestFields)
        {
            return this.PrepareSavedItemReload(this.InsertUser(requestFields));
        }
        protected override DataActionResult UpdateItem(object[] requestFields)
        {
            return this.PrepareSavedItemReload(this.UpdateUser(requestFields));
        }
        protected override DataActionResult DeleteItem(string key)
        {
            return this.DeleteUser(key);
        }
    }
}
