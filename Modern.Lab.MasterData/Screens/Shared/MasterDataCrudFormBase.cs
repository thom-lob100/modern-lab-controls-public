using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

using Modern.Lab.Hosting;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;

namespace Modern.Lab.MasterData
{
    public abstract class MasterDataCrudFormBase : ModernFormBase
    {
        private const string ChannelList = "list";

        private enum ListState
        {
            Loading,
            Ready,
            ReadyEmpty,
            Blocked,
            Failed
        }

        private enum WriteKind
        {
            New,
            Save,
            Delete
        }

        private MasterDataCrudDefinition definition;
        private MasterDataCrudView view;
        private TableContract listContract;
        private ListState listState = ListState.Loading;
        private TableResponse itemList;
        private bool blockedNoticeShowing;
        private string pendingSelectKey;

        protected void InitializeCrud(MasterDataCrudDefinition definition, MasterDataCrudView view)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }

            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            this.definition = definition;
            this.view = view;
            this.listContract = new TableContract(definition.ListTableId).Key(definition.KeyColumn);

            this.InitializeModernForm();
            this.view.ActionMenu.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
            this.DeferredResize = true;
            this.RegisterFindShortcut(this.view.Grid);

            this.view.Grid.RowKeyMember = definition.KeyColumn;
            this.view.Editor.KeyColumns = definition.KeyColumn;
            this.view.Editor.ParameterNameStyle = ParameterNameStyle.PascalCase;
            this.view.Editor.ValueChanged += this.OnEditorValueChanged;

            this.UpdateActionState();
        }

        protected Controls.ModernPropertyGrid CrudEditor
        {
            get { return this.view.Editor; }
        }

        protected virtual void LoadLookupSources()
        {
        }

        protected DataTable RequestLookupItems(string requestName, object[] requestFields)
        {
            return this.RequestFields(
                    this.definition.ActionName, WithMethodCommand(requestName, requestFields)).Table;
        }

        protected void ReloadItems()
        {
            string keyword = this.view.Keyword.Text.Trim();

            this.listState = ListState.Loading;
            this.UpdateActionState();

            this.LoadAsync(ChannelList,
                    () => this.RequestItems(keyword),
                    this.BindItems,
                    this.AfterListSettled);
        }

        protected void HandleSelectionChanged()
        {
            DataRow row = SelectedRow(this.view.Grid.SelectedItem);

            if (row != null && this.ListOpen)
            {
                this.view.Editor.LoadRow(row);
            }

            this.UpdateActionState();
        }

        protected void RefreshCrudActionState()
        {
            this.UpdateActionState();
        }

        protected void OnFormLoad(object sender, EventArgs e)
        {
            this.LoadLookupSources();
            this.ReloadItems();
        }

        protected void OnSearchClick(object sender, EventArgs e)
        {
            this.ReloadItems();
        }

        protected void OnKeywordEnterPressed(object sender, EventArgs e)
        {
            this.ReloadItems();
        }

        protected void OnNewClick(object sender, EventArgs e)
        {
            if (!this.CanWrite(WriteKind.New))
            {
                return;
            }

            this.view.Editor.BeginNew();
            this.view.Editor.FocusFirstEditor();
            this.UpdateActionState();
        }

        protected void OnCancelClick(object sender, EventArgs e)
        {
            if (!this.CanCancel)
            {
                return;
            }

            DataRow row = SelectedRow(this.view.Grid.SelectedItem);

            if (this.view.Editor.IsNew && row != null)
            {
                this.view.Editor.LoadRow(row);
            }
            else
            {
                this.view.Editor.RevertEdits();
            }

            this.UpdateActionState();
        }

        protected void OnSaveClick(object sender, EventArgs e)
        {
            if (!this.CanWrite(WriteKind.Save) || !this.ValidateRequired())
            {
                return;
            }

            object[] requestFields = this.view.Editor.ToRequestFields();
            string key = this.view.Editor.ReadKey().Trim();
            bool isNew = this.view.Editor.IsNew;

            this.RunAction(
                    () => isNew ? this.InsertItem(requestFields) : this.UpdateItem(requestFields),
                    reply => this.AfterSaved(reply, key),
                    isNew ? "Inserting " + this.EntityNameLower + "…" : "Updating " + this.EntityNameLower + "…");
        }

        protected void OnDeleteClick(object sender, EventArgs e)
        {
            if (!this.CanWrite(WriteKind.Delete))
            {
                return;
            }

            string key = this.view.Editor.ReadKey().Trim();

            if (!this.Confirm(
                    "Delete " + this.EntityNameLower + " '" + key + "'?",
                    "Delete " + this.definition.EntityName))
            {
                return;
            }

            this.RunAction(
                    () => this.DeleteItem(key),
                    this.AfterDeleted,
                    "Deleting " + this.EntityNameLower + "…");
        }

        protected void OnActionMenuOpening(object sender, CancelEventArgs e)
        {
            this.UpdateActionState();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && this.CanCancel && !this.view.Grid.IsFindPanelOpen)
            {
                this.OnCancelClick(this, EventArgs.Empty);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private string EntityNameLower
        {
            get { return this.definition.EntityName.ToLowerInvariant(); }
        }

        private bool ListOpen
        {
            get { return this.listState == ListState.Ready || this.listState == ListState.ReadyEmpty; }
        }

        private bool CanCancel
        {
            get
            {
                if (!this.ListOpen || this.ActionInProgress)
                {
                    return false;
                }

                return this.view.Editor.IsDirty
                        || (this.view.Editor.IsNew && this.view.Grid.SelectedItem != null);
            }
        }

        private bool SelectedRowValid
        {
            get
            {
                DataRow row = SelectedRow(this.view.Grid.SelectedItem);

                if (row == null || this.itemList == null || this.itemList.Table == null)
                {
                    return false;
                }

                int index = this.itemList.Table.Rows.IndexOf(row);
                return index >= 0 && this.itemList.IsRowValid(index);
            }
        }

        private DataTable RequestItems(string keyword)
        {
            return this.RequestFields(
                    this.definition.ActionName,
                    "MethodCommand", this.definition.SelectCommand,
                    "Keyword", keyword).Table;
        }

        private DataActionResult InsertItem(object[] requestFields)
        {
            return this.RequestFields(
                    this.definition.ActionName, WithMethodCommand(this.definition.InsertCommand, requestFields));
        }

        private DataActionResult UpdateItem(object[] requestFields)
        {
            return this.RequestFields(
                    this.definition.ActionName, WithMethodCommand(this.definition.UpdateCommand, requestFields));
        }

        private DataActionResult DeleteItem(string key)
        {
            return this.RequestFields(
                    this.definition.ActionName,
                    "MethodCommand", this.definition.DeleteCommand,
                    this.view.Editor.ParameterNameOf(this.definition.KeyColumn),
                    key);
        }

        private static object[] WithMethodCommand(string methodCommand, object[] fields)
        {
            int fieldCount = fields == null ? 0 : fields.Length;
            object[] result = new object[fieldCount + 2];
            result[0] = "MethodCommand";
            result[1] = methodCommand;

            if (fieldCount > 0)
            {
                Array.Copy(fields, 0, result, 2, fieldCount);
            }

            return result;
        }

        private void BindItems(DataTable table)
        {
            DataTable received = table ?? new DataTable();

            this.itemList = TableResponse.Read(
                    ResponseKind.Data, received, ColumnAliasCatalog.Empty, this.listContract);
            this.listState = this.ListStateOf(this.itemList);

            if (this.listState == ListState.Blocked)
            {
                this.pendingSelectKey = null;
                this.view.Editor.SetSchema(new DataTable());
                this.view.Editor.BeginNew();
                this.view.Grid.DataSource = received;
                this.ShowBlockedNotice();
                this.UpdateActionState();
                return;
            }

            this.HideBlockedNotice();

            DataTable bound = this.itemList.Table;
            this.view.Editor.SetSchema(bound);
            this.view.Grid.DataSource = bound;

            if (!string.IsNullOrEmpty(this.pendingSelectKey))
            {
                this.SelectByKey(bound, this.pendingSelectKey);
                this.pendingSelectKey = null;
            }

            if (this.view.Grid.SelectedItem == null)
            {
                this.view.Editor.BeginNew();
            }
            else
            {
                this.view.Editor.LoadRow(SelectedRow(this.view.Grid.SelectedItem));
            }

            this.UpdateActionState();
        }

        private void AfterListSettled(bool current)
        {
            if (!current || this.listState != ListState.Loading)
            {
                return;
            }

            this.pendingSelectKey = null;
            this.itemList = TableResponse.Read(
                    ResponseKind.Failed,
                    null,
                    ColumnAliasCatalog.Empty,
                    this.listContract,
                    string.Empty,
                    this.definition.EntityName + " list could not be loaded.");
            this.listState = ListState.Failed;
            this.UpdateActionState();
        }

        private ListState ListStateOf(TableResponse response)
        {
            if (response.State == TableResponseState.Failed)
            {
                return ListState.Failed;
            }

            if (response.ReceivedColumns.Count == 0
                    || response.State == TableResponseState.MissingRequired
                    || this.MissingEditorColumns(response).Length > 0
                    || this.ReservedEditorColumns(response).Length > 0)
            {
                return ListState.Blocked;
            }

            return response.State == TableResponseState.Empty ? ListState.ReadyEmpty : ListState.Ready;
        }

        private string[] MissingEditorColumns(TableResponse response)
        {
            List<string> missing = new List<string>();
            string[] required = this.definition.RequiredEditorColumns ?? new string[0];

            foreach (string column in required)
            {
                bool found = false;

                foreach (string received in response.ReceivedColumns)
                {
                    if (string.Equals(received, column, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    missing.Add(column);
                }
            }

            return missing.ToArray();
        }

        private string[] ReservedEditorColumns(TableResponse response)
        {
            List<string> reserved = new List<string>();

            foreach (string received in response.ReceivedColumns)
            {
                string parameterName = ParameterNameConverter.Convert(
                        received, this.view.Editor.ParameterNameStyle);

                if (string.Equals(parameterName, "MethodCommand", StringComparison.OrdinalIgnoreCase))
                {
                    reserved.Add(received);
                }
            }

            return reserved.ToArray();
        }

        private bool CanWrite(WriteKind kind)
        {
            if (!this.ListOpen || this.ActionInProgress)
            {
                return false;
            }

            if (kind == WriteKind.New)
            {
                return true;
            }

            if (this.view.Editor.IsNew)
            {
                return kind == WriteKind.Save;
            }

            return this.SelectedRowValid;
        }

        private void ShowBlockedNotice()
        {
            List<string> details = new List<string>();
            List<string> missing = new List<string>(ToArray(this.itemList.MissingColumns));
            missing.AddRange(this.MissingEditorColumns(this.itemList));

            if (missing.Count > 0)
            {
                details.Add("missing " + string.Join(", ", missing.ToArray()));
            }

            string[] reserved = this.ReservedEditorColumns(this.itemList);

            if (reserved.Length > 0)
            {
                details.Add("reserved " + string.Join(", ", reserved));
            }

            string text = this.itemList.ReceivedColumns.Count == 0
                    ? this.definition.EntityName + " list did not match the contract (no columns) — writes are disabled."
                    : this.definition.EntityName + " list did not match the contract ("
                            + string.Join("; ", details.ToArray()) + ") — writes are disabled.";

            this.blockedNoticeShowing = true;
            this.ShowStickyNotice(text, Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
        }

        private void HideBlockedNotice()
        {
            if (!this.blockedNoticeShowing)
            {
                return;
            }

            this.blockedNoticeShowing = false;
            this.HideNotice();
        }

        private static string[] ToArray(IList<string> values)
        {
            string[] result = new string[values.Count];
            values.CopyTo(result, 0);
            return result;
        }

        private void SelectByKey(DataTable table, string key)
        {
            if (table == null || !table.Columns.Contains(this.definition.KeyColumn))
            {
                return;
            }

            for (int i = 0; i < table.Rows.Count; i = i + 1)
            {
                if (string.Equals(
                        Convert.ToString(table.Rows[i][this.definition.KeyColumn]),
                        key,
                        StringComparison.OrdinalIgnoreCase))
                {
                    this.view.Grid.SelectedIndex = i;
                    return;
                }
            }
        }

        private static DataRow SelectedRow(object item)
        {
            DataRowView view = item as DataRowView;

            if (view != null)
            {
                return view.Row;
            }

            return item as DataRow;
        }

        private void OnEditorValueChanged(object sender, EventArgs e)
        {
            this.UpdateActionState();
        }

        private bool ValidateRequired()
        {
            string[] missing = this.view.Editor.MissingRequired();

            if (missing.Length == 0)
            {
                return true;
            }

            List<string> captions = new List<string>();

            foreach (string column in missing)
            {
                captions.Add(this.view.Editor.CaptionOf(column));
            }

            this.ShowToast(
                    "Required: " + string.Join(", ", captions.ToArray()),
                    Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
            this.view.Editor.FocusFirstEditor();

            return false;
        }

        private void AfterSaved(DataActionResult reply, string selectKey)
        {
            this.ShowToast(reply.Message.Length > 0 ? reply.Message : "Done.");
            this.pendingSelectKey = ReturnedKey(reply) ?? selectKey;
            this.ReloadItems();
        }

        private void AfterDeleted(DataActionResult reply)
        {
            this.ShowToast(reply.Message.Length > 0 ? reply.Message : "Done.");
            this.pendingSelectKey = null;
            this.ReloadItems();
        }

        private static string ReturnedKey(DataActionResult reply)
        {
            if (reply == null || reply.Data.Length == 0)
            {
                return null;
            }

            string[] values = reply.Values;

            return values.Length > 0 && values[0].Trim().Length > 0 ? values[0].Trim() : null;
        }

        private void UpdateActionState()
        {
            this.view.Editor.Enabled = this.ListOpen;
            this.view.NewButton.Enabled = this.CanWrite(WriteKind.New);
            this.view.CancelButton.Enabled = this.CanCancel;
            this.view.SaveButton.Enabled = this.CanWrite(WriteKind.Save);
            this.view.DeleteButton.Enabled = this.CanWrite(WriteKind.Delete);
            this.view.NewMenuItem.Enabled = this.view.NewButton.Enabled;
            this.view.CancelMenuItem.Enabled = this.view.CancelButton.Enabled;
            this.view.SaveMenuItem.Enabled = this.view.SaveButton.Enabled;
            this.view.DeleteMenuItem.Enabled = this.view.DeleteButton.Enabled;
            this.view.EditorCard.Text = this.view.Editor.IsNew
                    ? this.definition.EntityName + " — New"
                    : this.definition.EntityName + " — " + this.view.Editor.ReadKey();
        }
    }
}
