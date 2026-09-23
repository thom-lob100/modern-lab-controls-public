using System;
using System.Data;
using System.Collections.Generic;
using System.Windows.Forms;

using Modern.Lab.Hosting;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;

namespace Modern.Lab.MasterData
{
    public partial class CommonCodeForm : ModernFormBase
    {
        private enum ListState { NoParent, Loading, Ready, Failed }

        private readonly bool manageTypes;
        private bool bindingTypes;
        private bool loadingTypes;
        private bool typeSelectionQueued;
        private System.Windows.Controls.TextChangedEventHandler typeTextChanged;
        private string commonTyp = string.Empty;
        private bool saving;

        public CommonCodeForm() : this(false)
        {
        }

        internal CommonCodeForm(bool manageTypes)
        {
            this.manageTypes = manageTypes;
            this.InitializeComponent();
            this.InitializeModernForm();
            this.DeferredResize = true;
            this.ConfigurePane(this.editorPane);
            if (!this.manageTypes)
            {
                this.cmbCommonType.SelectedIndexChanged += this.OnCommonTypeChanged;
                this.btnRefreshTypes.Click += (sender, e) => this.LoadTypes(false);
                this.typeTextChanged = this.OnCommonTypeTextChanged;
                this.cmbCommonType.Child.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                        this.typeTextChanged, true);
            }
            this.UpdateActions();
            this.Load += (sender, e) =>
            {
                if (this.manageTypes)
                {
                    this.Reload(this.editorPane, null);
                }
                else
                {
                    this.LoadTypes(true);
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.typeTextChanged != null && this.cmbCommonType.Child != null)
            {
                this.cmbCommonType.Child.RemoveHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                        this.typeTextChanged);
                this.typeTextChanged = null;
            }
            base.Dispose(disposing);
        }

        private void ConfigurePane(CodePane pane)
        {
            this.DefineEditors(pane);
            pane.Grid.SelectionChanged += (sender, e) => this.SelectRow(pane);
            pane.Editor.ValueChanged += (sender, e) => this.UpdateActions();
            pane.Search.Click += (sender, e) => this.Reload(pane, null);
            pane.Keyword.EnterPressed += (sender, e) => this.Reload(pane, null);
            pane.New.Click += (sender, e) => this.BeginNew(pane);
            pane.Cancel.Click += (sender, e) => this.CancelEdit(pane);
            pane.Save.Click += (sender, e) => this.SaveItem(pane);
            pane.Delete.Click += (sender, e) => this.DeleteItem(pane);
            this.RegisterFindShortcut(pane.Grid);
        }

        private void ClearPane(CodePane pane, ListState state)
        {
            pane.Binding = true;
            pane.State = state;
            pane.Table = null;
            pane.StoredKey = string.Empty;
            pane.Grid.DataSource = null;
            pane.Editor.SetSchema(new DataTable());
            pane.Editor.BeginNew();
            pane.Binding = false;
            pane.Grid.EmptyText = state == ListState.Loading ? "Loading…" : "Select a saved common type.";
        }

        private void Reload(CodePane pane, string selectKey)
        {
            if (this.saving || this.ActionInProgress || this.loadingTypes
                    || (!this.manageTypes && (this.commonTyp.Length == 0 || this.commonTyp != this.SelectedCommonTyp)))
            {
                return;
            }
            string parent = this.commonTyp;
            string keyword = pane.Keyword.Text;
            this.ClearPane(pane, ListState.Loading);
            this.UpdateActions();
            bool isDetail = !this.manageTypes;
            this.LoadAsync(pane.Channel,
                    () => this.SelectItems(isDetail, parent, keyword),
                    table => this.BindItems(pane, table, selectKey),
                    current => this.AfterLoaded(pane, current));
            this.UpdateActions();
        }

        private void BindItems(CodePane pane, DataTable table, string selectKey)
        {
            pane.Binding = true;
            pane.Table = table;
            pane.State = ListState.Ready;
            pane.Editor.SetSchema(table);
            pane.Grid.EmptyText = "No matching items.";
            pane.Grid.DataSource = table;
            int selected = table.Rows.Count == 0 ? -1 : 0;
            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (string.Equals(Convert.ToString(table.Rows[i][pane.KeyColumn]), selectKey, StringComparison.OrdinalIgnoreCase))
                {
                    selected = i;
                    break;
                }
            }
            pane.Grid.SelectedIndex = selected;
            pane.Binding = false;
            this.SelectRow(pane);
        }

        private void AfterLoaded(CodePane pane, bool current)
        {
            if (current && pane.State == ListState.Loading)
            {
                pane.State = ListState.Failed;
                pane.Grid.EmptyText = "Could not load items. Search to retry.";
            }
            this.PostToUi(this.UpdateActions);
        }

        private void SelectRow(CodePane pane)
        {
            if (pane.Binding || this.saving || pane.State != ListState.Ready)
            {
                return;
            }
            DataRowView view = pane.Grid.SelectedItem as DataRowView;
            DataRow row = view == null ? pane.Grid.SelectedItem as DataRow : view.Row;
            if (row == null)
            {
                this.LoadNewValues(pane);
            }
            else
            {
                pane.NewMode = false;
                pane.StoredKey = Convert.ToString(row[pane.KeyColumn]);
                this.SetEditorMode(pane, false);
                pane.Editor.LoadRow(row);

            }
            this.UpdateActions();
        }

        private void LoadNewValues(CodePane pane)
        {
            pane.NewMode = true;
            pane.StoredKey = string.Empty;
            this.SetEditorMode(pane, true);
            DataRow row = pane.Table.NewRow();
            row["SORT_NO"] = "0";
            row["USE_YN"] = "Y";
            if (!this.manageTypes)
            {
                row["COMMON_TYP"] = this.commonTyp;
            }
            pane.Editor.LoadRow(row);
        }

        private void SetEditorMode(CodePane pane, bool isNew)
        {
            string keys = isNew ? string.Empty : !this.manageTypes ? "COMMON_TYP,TYP_VAL" : "COMMON_TYP";
            if (pane.Editor.KeyColumns != keys)
            {
                pane.Editor.SetSchema(new DataTable());
            }
            pane.Editor.KeyColumns = keys;
            pane.Editor.SetSchema(pane.Table);
        }

        private void BeginNew(CodePane pane)
        {
            if (!this.CanEdit(pane))
            {
                return;
            }
            pane.Binding = true;
            pane.Grid.SelectedIndex = -1;
            pane.Binding = false;
            this.LoadNewValues(pane);
            pane.Editor.FocusFirstEditor();
            this.UpdateActions();
        }

        private void CancelEdit(CodePane pane)
        {
            if (this.CanEdit(pane))
            {
                pane.Editor.RevertEdits();
                this.UpdateActions();
            }
        }

        private bool CanEdit(CodePane pane)
        {
            return !this.saving && !this.ActionInProgress && pane.State == ListState.Ready
                    && (this.manageTypes || (this.commonTyp.Length > 0 && this.commonTyp == this.SelectedCommonTyp));
        }

        private void SaveItem(CodePane pane)
        {
            if (!this.CanEdit(pane) || !this.CanStartAction())
            {
                return;
            }
            string key = pane.NewMode ? pane.Editor.ReadValue(pane.KeyColumn).Trim() : pane.StoredKey;
            if (key.Length == 0)
            {
                this.ShowErrorMessage("Save", "Enter an identifier before saving.", string.Empty);
                return;
            }
            bool isDetail = !this.manageTypes;
            string parent = isDetail ? this.commonTyp : key;
            object[] fields = pane.Editor.ToRequestFields();
            string method = (pane.NewMode ? "Insert" : "Update") + (isDetail ? "CommonCode" : "CommonType");
            this.RunWrite(pane, () => this.WriteItem(method, parent, isDetail ? key : null, fields), key);
        }

        private void DeleteItem(CodePane pane)
        {
            if (!this.CanEdit(pane) || pane.NewMode || !this.CanStartAction())
            {
                return;
            }
            string key = pane.StoredKey;
            bool isDetail = !this.manageTypes;
            string parent = isDetail ? this.commonTyp : key;
            if (!this.Confirm("Delete " + key + "?", "Delete"))
            {
                return;
            }
            this.RunWrite(pane, () => this.WriteItem(isDetail ? "DeleteCommonCode" : "DeleteCommonType",
                    parent, isDetail ? key : null, new object[0]), null);
        }

        private void RunWrite(CodePane pane, Func<DataActionResult> call, string selectKey)
        {
            this.saving = true;
            this.UpdateActions();
            this.RunAction(call, reply =>
            {
                this.saving = false;
                if (selectKey != null)
                {
                    pane.Keyword.Text = string.Empty;
                }
                this.ShowToast("Changes saved.");
                this.Reload(pane, selectKey);
            }, reply =>
            {
                this.saving = false;
                this.UpdateActions();
                this.ShowActionFailure(reply);
            }, "Saving common codes…");
        }

        private void UpdateActions()
        {
            if (this.editorPane == null)
            {
                return;
            }
            CodePane pane = this.editorPane;
            bool editable = this.CanEdit(pane);
            pane.Editor.Enabled = editable;
            pane.Grid.Enabled = editable;
            pane.Keyword.Enabled = !this.saving;
            pane.Search.Enabled = !this.saving && !this.loadingTypes
                    && (this.manageTypes || this.SelectedCommonTyp.Length > 0);
            pane.New.Enabled = editable;
            pane.Cancel.Enabled = editable && pane.Editor.IsDirty;
            pane.Save.Enabled = editable && !this.QueryInProgress;
            pane.Delete.Enabled = editable && !pane.NewMode && !this.QueryInProgress;
            this.cmbCommonType.Enabled = !this.saving && !this.loadingTypes;
            this.btnRefreshTypes.Enabled = !this.saving && !this.loadingTypes;
        }

        private string SelectedCommonTyp
        {
            get
            {
                DataRowView view = this.cmbCommonType.SelectedItem as DataRowView;
                DataRow row = view == null ? this.cmbCommonType.SelectedItem as DataRow : view.Row;
                if (row == null || !string.Equals(this.cmbCommonType.Text,
                        Convert.ToString(row[this.cmbCommonType.DisplayMember]), StringComparison.Ordinal))
                {
                    return string.Empty;
                }
                return Convert.ToString(row[this.cmbCommonType.ValueMember]);
            }
        }

        private void InvalidateCodes()
        {
            this.commonTyp = string.Empty;
            this.InvalidateChannel(this.editorPane.Channel);
            this.ClearPane(this.editorPane, ListState.NoParent);
            this.UpdateActions();
        }

        private void OnCommonTypeTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.OnCommonTypeChanged(sender, EventArgs.Empty);
        }

        private void OnCommonTypeChanged(object sender, EventArgs e)
        {
            if (this.bindingTypes || this.loadingTypes || this.saving)
            {
                return;
            }
            this.InvalidateCodes();
            this.QueueTypeSelection();
        }

        private void QueueTypeSelection()
        {
            if (this.typeSelectionQueued)
            {
                return;
            }
            this.typeSelectionQueued = true;
            this.PostToUi(() =>
            {
                this.typeSelectionQueued = false;
                if (this.loadingTypes || this.saving)
                {
                    return;
                }
                string selected = this.SelectedCommonTyp;
                if (selected.Length == 0)
                {
                    this.InvalidateCodes();
                    return;
                }
                this.commonTyp = selected;
                this.editorPane.Keyword.Text = string.Empty;
                this.Reload(this.editorPane, null);
            });
        }

        private void LoadTypes(bool selectFirst)
        {
            if (this.saving || this.loadingTypes)
            {
                return;
            }
            string previous = this.commonTyp;
            this.loadingTypes = true;
            this.bindingTypes = true;
            this.cmbCommonType.DataSource = null;
            this.cmbCommonType.Text = string.Empty;
            this.bindingTypes = false;
            this.InvalidateCodes();
            this.editorPane.Grid.EmptyText = "Loading common types…";
            bool applied = false;
            this.LoadAsync("types", () => this.SelectItems(false, string.Empty, string.Empty), table =>
            {
                DataTable choices = new DataTable();
                choices.Columns.Add("COMMON_TYP");
                choices.Columns.Add("DISPLAY_NAME");
                int selected = selectFirst && table.Rows.Count > 0 ? 0 : -1;
                foreach (DataRow row in table.Rows)
                {
                    string key = Convert.ToString(row["COMMON_TYP"]);
                    choices.Rows.Add(key, key + " — " + Convert.ToString(row["COMMON_NM"]));
                    if (string.Equals(key, previous, StringComparison.OrdinalIgnoreCase))
                    {
                        selected = choices.Rows.Count - 1;
                    }
                }
                this.bindingTypes = true;
                try
                {
                    this.cmbCommonType.DataSource = choices;
                    this.cmbCommonType.SelectedIndex = selected;
                    if (selected < 0)
                    {
                        this.cmbCommonType.Text = string.Empty;
                    }
                }
                finally
                {
                    this.bindingTypes = false;
                }
                applied = true;
            }, current =>
            {
                if (!current)
                {
                    return;
                }
                this.loadingTypes = false;
                this.editorPane.Grid.EmptyText = applied ? "Select a saved common type."
                        : "Could not load common types. Click Refresh Types to retry.";
                if (applied)
                {
                    this.QueueTypeSelection();
                }
                this.PostToUi(this.UpdateActions);
            });
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
        private static readonly string[] MasterColumns =
        {
            "COMMON_TYP", "COMMON_NM", "DESCRIPTION", "SORT_NO", "USE_YN", "UPDATED_BY", "UPDATED_AT"
        };
        private static readonly string[] DetailColumns =
        {
            "COMMON_TYP", "TYP_VAL", "TYP_NM", "DESCRIPTION", "SORT_NO", "USE_YN", "UPDATED_BY", "UPDATED_AT"
        };

        private DataTable SelectItems(bool isDetail, string commonTyp, string keyword)
        {
            DataTable table = this.RequestItems(isDetail, commonTyp, keyword);
            string missing = ResponseColumns.Missing(table, isDetail ? DetailColumns : MasterColumns);
            if (missing.Length > 0)
            {
                throw new ResponseContractException("Common codes — missing columns: " + missing);
            }
            HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in table.Rows)
            {
                string parent = Convert.ToString(row["COMMON_TYP"]);
                string key = Convert.ToString(row[isDetail ? "TYP_VAL" : "COMMON_TYP"]);
                if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(key) || !keys.Add(key)
                        || (isDetail && !string.Equals(parent, commonTyp, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ResponseContractException("Common codes — empty/duplicate key or mismatched COMMON_TYP.");
                }
            }
            return table;
        }
    }
}
