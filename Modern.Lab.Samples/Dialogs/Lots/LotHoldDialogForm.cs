using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using Modern.Lab.Data;
using Modern.Lab.Hosting;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.WinForms.Controls.Selection;

namespace Modern.Lab.Samples
{
    public partial class LotHoldDialogForm : ModernFormBase
    {
        private const string lookupChannel = "holdLookups";
        private const string engineerChannel = "holdEngineers";
        private readonly DataRow lot;
        private readonly bool releaseMode;
        private bool ready;
        private bool closed;
        private bool refreshPending;
        private bool bindingEngineers;
        private bool engineerCandidatesReady;
        private int engineerVersion;

        public LotHoldDialogForm()
        {
            this.InitializeComponent();
            this.InitializeModernForm(false);
            if (this.cboCode.Child != null)
            {
                this.cboCode.Child.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                        new System.Windows.Controls.TextChangedEventHandler(this.OnComboTextChanged), true);
            }
            if (this.cboEngineer.Child != null)
            {
                this.cboEngineer.Child.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                        new System.Windows.Controls.TextChangedEventHandler(this.OnEngineerTextChanged), true);
            }
        }

        public LotHoldDialogForm(DataRow lot, bool releaseMode) : this()
        {
            if (lot == null) { throw new ArgumentNullException(nameof(lot)); }
            DataTable copy = lot.Table.Clone();
            copy.Rows.Add(lot.ItemArray);
            this.lot = copy.Rows[0];
            this.releaseMode = releaseMode;
            this.CurrentOperId = TableHelper.CellText(this.lot, ServerFields.Item.OperId).Trim();
        }

        public string CurrentOperId { get; private set; }
        public string ReasonCode { get; private set; }
        public string EngrUserId { get; private set; }
        public string Description { get; private set; }

        private async void OnFormLoad(object sender, EventArgs e)
        {
            if (IsDesignTime() || this.DesignMode || this.lot == null) { return; }
            this.Text = (this.releaseMode ? "NotOnHold" : "Hold") + " — "
                    + TableHelper.CellText(this.lot, ServerFields.Lot.LotId);
            this.lblCode.Text = this.releaseMode ? "Release Code" : "Hold Code";
            this.btnOk.Text = this.releaseMode ? "NotOnHold" : "Hold";
            SourceDefinitions(this.lot.Table).Apply(this.fieldSource);
            this.fieldSource.SetRow(this.lot);
            ConfigureCodes(this.cboCode);
            ConfigureEngineers(this.cboEngineer);
            this.SetLookupReady(false);
            if (string.IsNullOrWhiteSpace(this.CurrentFacId))
            {
                this.lblStatus.Text = "Sign in with a facility to continue.";
                this.ShowErrorMessage("Lookup unavailable", this.lblStatus.Text, "CurrentFacId is empty.");
                return;
            }
            LoadOutcome<DataTable> outcome = await this.FetchAsync(lookupChannel,
                    () => this.RequestReasonCodes(this.releaseMode));
            if (!outcome.IsCurrent || this.closed || this.IsDisposed) { return; }
            if (outcome.Failure != null)
            {
                this.lblStatus.Text = "Lookup failed. Close and try again.";
                return;
            }
            string missingCodes = outcome.Value != null && outcome.Value.Rows.Count == 0 ? string.Empty
                    : ResponseColumns.Missing(outcome.Value, "REASON_CD", "CTN_DESC");
            if (missingCodes.Length > 0)
            {
                this.lblStatus.Text = "The selection lists could not be loaded.";
                this.ShowErrorMessage("Lookup unavailable", this.lblStatus.Text,
                        "Reason codes: " + missingCodes);
                return;
            }
            this.cboCode.DataSource = outcome.Value;
            this.cboCode.SelectedIndex = -1;
            this.cboEngineer.SelectedIndex = -1;
            this.SetLookupReady(true);
            this.lblStatus.Text = outcome.Value.Rows.Count == 0 ? "No reason codes are registered."
                    : "Type user id or name.";
        }

        private void SetLookupReady(bool value)
        {
            this.ready = value;
            this.cboCode.Enabled = value;
            this.cboEngineer.Enabled = value;
            this.RefreshSelection();
        }

        private static string SelectedKey(ModernComboBox combo)
        {
            DataRowView selected = combo.SelectedItem as DataRowView;
            if (selected == null || !string.Equals(combo.Text,
                    TableHelper.CellText(selected.Row, combo.DisplayMember), StringComparison.Ordinal))
            {
                return string.Empty;
            }
            return TableHelper.CellText(selected.Row, combo.ValueMember).Trim();
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (this.bindingEngineers) { return; }
            this.QueueSelectionRefresh();
        }

        private async void OnEngineerTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!this.ready || this.bindingEngineers || this.closed || this.IsDisposed || this.Disposing) { return; }
            System.Windows.Controls.TextBox editor = e.OriginalSource as System.Windows.Controls.TextBox;
            string input = editor == null ? this.cboEngineer.Text : editor.Text;
            DataRowView selected = this.cboEngineer.SelectedItem as DataRowView;
            if (this.engineerCandidatesReady && selected != null
                    && string.Equals(input, TableHelper.CellText(selected.Row, this.cboEngineer.DisplayMember), StringComparison.Ordinal))
            {
                this.QueueSelectionRefresh();
                return;
            }

            int version = ++this.engineerVersion;
            this.InvalidateChannel(engineerChannel);
            this.engineerCandidatesReady = false;
            this.EngrUserId = string.Empty;
            this.lblEngineerName.Text = string.Empty;
            this.btnOk.Enabled = false;
            await Task.Yield();
            if (!this.IsCurrentEngineerSearch(version)) { return; }
            if (this.cboEngineer.DataSource != null && input.Length > 0
                    && string.Equals(input, this.cboEngineer.Text, StringComparison.Ordinal)
                    && SelectedKey(this.cboEngineer).Length > 0)
            {
                this.engineerCandidatesReady = true;
                this.RefreshSelection();
                return;
            }
            this.BindEngineers(null, input, editor);
            string keyword = input.Trim();
            this.lblStatus.Text = keyword.Length == 0 ? "Type user id or name." : "Searching users...";
            if (keyword.Length == 0) { return; }

            await Task.Delay(300);
            if (!this.IsCurrentEngineerSearch(version)) { return; }
            LoadOutcome<DataTable> outcome = await this.FetchAsync(engineerChannel, () => this.RequestEngineers(keyword));
            if (!outcome.IsCurrent || !this.IsCurrentEngineerSearch(version)) { return; }
            if (outcome.Failure != null)
            {
                this.lblStatus.Text = "User lookup failed. Change the search text to try again.";
                return;
            }
            string missing = outcome.Value != null && outcome.Value.Rows.Count == 0 ? string.Empty
                    : ResponseColumns.Missing(outcome.Value, "USER_ID", "USER_NM");
            if (missing.Length > 0)
            {
                this.lblStatus.Text = "The user list could not be loaded.";
                this.ShowErrorMessage("Lookup unavailable", this.lblStatus.Text, "Engineers: " + missing);
                return;
            }
            this.BindEngineers(outcome.Value, input, editor);
            this.engineerCandidatesReady = true;
            this.lblStatus.Text = outcome.Value.Rows.Count == 0 ? "No matching users." : "Select a user.";
            this.RefreshSelection();
        }

        private bool IsCurrentEngineerSearch(int version)
        {
            return this.ready && !this.closed && !this.IsDisposed && !this.Disposing && version == this.engineerVersion;
        }

        protected override void OnLoadFailed(string channel, Exception failure)
        {
            if (channel == engineerChannel)
            {
                this.ShowErrorMessage("Lookup failed", "User lookup failed. Change the search text to try again.", failure.Message);
                return;
            }
            base.OnLoadFailed(channel, failure);
        }

        private void BindEngineers(DataTable users, string input, System.Windows.Controls.TextBox editor)
        {
            int selectionStart = editor == null ? 0 : editor.SelectionStart;
            int selectionLength = editor == null ? 0 : editor.SelectionLength;
            this.bindingEngineers = true;
            try
            {
                this.cboEngineer.DataSource = users;
                this.cboEngineer.SelectedIndex = -1;
                this.cboEngineer.Text = input;
                if (editor != null)
                {
                    editor.Select(Math.Min(selectionStart, input.Length), Math.Min(selectionLength, input.Length - Math.Min(selectionStart, input.Length)));
                }
            }
            finally
            {
                this.bindingEngineers = false;
            }
        }

        private void OnComboTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.QueueSelectionRefresh();
        }

        private void QueueSelectionRefresh()
        {
            if (this.closed || this.IsDisposed || !this.IsHandleCreated || this.refreshPending) { return; }
            this.refreshPending = true;
            this.BeginInvoke(new MethodInvoker(delegate
            {
                this.refreshPending = false;
                if (!this.closed && !this.IsDisposed) { this.RefreshSelection(); }
            }));
        }

        private void RefreshSelection()
        {
            string engineer = this.engineerCandidatesReady ? SelectedKey(this.cboEngineer) : string.Empty;
            this.EngrUserId = engineer;
            DataRowView row = this.cboEngineer.SelectedItem as DataRowView;
            this.lblEngineerName.Text = engineer.Length == 0 || row == null ? string.Empty
                    : TableHelper.CellText(row.Row, "USER_NM");
            this.btnOk.Enabled = this.ready && !this.closed && engineer.Length > 0
                    && SelectedKey(this.cboCode).Length > 0
                    && !string.IsNullOrWhiteSpace(this.CurrentOperId)
                    && TableHelper.CellText(this.lot, ServerFields.Lot.LotId).Trim().Length > 0;
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            this.RefreshSelection();
            if (!this.btnOk.Enabled) { return; }
            this.ReasonCode = SelectedKey(this.cboCode);
            this.EngrUserId = SelectedKey(this.cboEngineer);
            this.Description = this.txtDescription.Text ?? string.Empty;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            this.closed = true;
            this.ready = false;
            this.engineerVersion++;
            this.InvalidateChannel(lookupChannel);
            this.InvalidateChannel(engineerChannel);
        }
    }
}
