using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Modern.Lab.Hosting;
using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.Controls.Wpf.Display;
using Modern.Lab.Data;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.WinForms.Controls.Data;

using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.Samples
{

    public partial class LogisticsRequestForm : ModernFormBase
    {
        private DataTable resultData;

        private List<DataRow> viewRows;

        private const string channelBoard = "board";
        private const string channelWafers = "wafers";

        private int elapsedOptionCount;

        private bool boardSelectionSilent;

        private string boardColumnSchema = string.Empty;

        public LogisticsRequestForm()
        {
            this.InitializeComponent();

            this.InitializeModernForm(this.midPanel);

            this.DeferredResize = true;

            this.RegisterFindShortcut(this.gridBoard, this.gridWafers);

            this.badgeSent.ColorValue = LogisticsRequestPresenter.StatusNames[LogisticsRequestPresenter.StatusSent];
            this.badgeReceived.ColorValue = LogisticsRequestPresenter.StatusNames[LogisticsRequestPresenter.StatusReceived];
            this.badgeUnlinked.ColorValue = LogisticsRequestPresenter.StatusNames[LogisticsRequestPresenter.StatusUnlinked];
            this.badgeLinked.ColorValue = LogisticsRequestPresenter.StatusNames[LogisticsRequestPresenter.StatusLinked];
            this.badgeCompleted.ColorValue = LogisticsRequestPresenter.StatusNames[LogisticsRequestPresenter.StatusCompleted];
            this.badgeUnmatched.Semantic = Modern.Lab.Theming.SemanticKind.Error;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            DataTable statusTable = new DataTable();
            statusTable.Columns.Add("VALUE", typeof(string));
            statusTable.Columns.Add("LABEL", typeof(string));

            foreach (string statusName in LogisticsRequestPresenter.StatusNames)
            {
                statusTable.Rows.Add(statusName, statusName);
            }

            this.cboStatus.DisplayMember = "LABEL";
            this.cboStatus.ValueMember = "VALUE";
            this.cboStatus.PlaceholderText = "All";
            this.cboStatus.DataSource = statusTable;

            DataTable sendTable = new DataTable();
            sendTable.Columns.Add("VALUE", typeof(string));
            sendTable.Columns.Add("LABEL", typeof(string));
            sendTable.Rows.Add("", "All");
            sendTable.Rows.Add("Y", "Y");
            sendTable.Rows.Add("N", "N");

            this.tglSend.DisplayMember = "LABEL";
            this.tglSend.ValueMember = "VALUE";
            this.tglSend.DataSource = sendTable;
            this.tglSend.SelectedValue = "";

            DataTable elapsedTable = new DataTable();
            elapsedTable.Columns.Add("DAYS", typeof(string));
            elapsedTable.Columns.Add("LABEL", typeof(string));
            elapsedTable.Rows.Add("1", "1+ days");
            elapsedTable.Rows.Add("3", "3+ days");
            elapsedTable.Rows.Add("7", "7+ days");
            elapsedTable.Rows.Add("14", "14+ days");

            this.cboElapsed.DisplayMember = "LABEL";
            this.cboElapsed.ValueMember = "DAYS";
            this.cboElapsed.PlaceholderText = "All";
            this.cboElapsed.DataSource = elapsedTable;
            this.elapsedOptionCount = elapsedTable.Rows.Count;

            this.cboLotId.DisplayMember = ServerFields.Lot.LotId;
            this.cboLotId.ValueMember = ServerFields.Lot.LotId;

            this.gridBoard.RowKeyMember = ServerFields.Lot.LotId;
            this.gridBoard.RestoreSelectionByIndex = true;

            this.ExecuteSearch();
        }

        private void OnSearchClick(object sender, EventArgs e)
        {
            this.ExecuteSearch();
        }

        private void OnResetClick(object sender, EventArgs e)
        {
            this.cboLotId.Text = string.Empty;
            this.cboStatus.UncheckAll();
            this.tglSend.SelectedValue = "";
            this.cboElapsed.UncheckAll();
            this.tpSentAfter.Value = null;
            this.ExecuteSearch();
        }

        private List<string> GetStatusFilter()
        {
            return CheckedStrings(this.cboStatus);
        }

        private List<string> GetSendFilter()
        {
            List<string> filter = new List<string>();
            string value = this.tglSend.SelectedValue as string;

            if (!string.IsNullOrEmpty(value))
            {
                filter.Add(value);
            }

            return filter;
        }

        private int GetMinDays()
        {
            List<string> checkedValues = CheckedStrings(this.cboElapsed);

            if (checkedValues.Count == 0 || checkedValues.Count >= elapsedOptionCount)
            {
                return 0;
            }

            int minDays = 0;

            foreach (string value in checkedValues)
            {
                int days = TableHelper.ParseInt(value);

                if (days > 0 && (minDays == 0 || days < minDays))
                {
                    minDays = days;
                }
            }

            return minDays;
        }

        private static List<string> CheckedStrings(
                Modern.Lab.WinForms.Controls.Selection.ModernCheckComboBox combo)
        {
            List<string> values = new List<string>();

            foreach (object value in combo.CheckedValues)
            {
                string text = value == null ? string.Empty : value.ToString();

                if (text.Length > 0)
                {
                    values.Add(text);
                }
            }

            return values;
        }

        private void ExecuteSearch()
        {
            this.ExecuteSearch(string.Empty);
        }

        private async void ExecuteSearch(
                string focusLotId, bool silent = false)
        {
            string keyword = this.cboLotId.Text.Trim();
            List<string> statusFilter = this.GetStatusFilter();
            List<string> sendFilter = this.GetSendFilter();
            int minDays = this.GetMinDays();
            TimeSpan? sentAfter = this.tpSentAfter.Value;

            IDisposable busy = silent
                    ? null
                    : this.Busy("Searching requests...", "Checking transfer status");

            this.InvalidateChannel(channelWafers);

            LoadOutcome<DataTable> outcome;

            try
            {
                outcome = await this.FetchAsync(
                        channelBoard,
                        () => this.RequestBoard(keyword, BoardStatusFilter(statusFilter)),
                        silent);
            }
            finally
            {
                if (busy != null)
                {
                    busy.Dispose();
                }
            }

            if (!outcome.IsCurrent)
            {
                return;
            }

            if (outcome.Failure != null)
            {
                return;
            }

            DataTable board = outcome.Value;

            DateTime daysBasis = DateTime.Now;

            LogisticsRequestPresenter.ApplyWorkflowColumns(board, ApplyActionRules);

            if (this.resultData != null)
            {
                this.resultData.ColumnChanged -= this.OnResultColumnChanged;
            }

            this.resultData = LogisticsRequestPresenter.Filter(
                    board, statusFilter, sendFilter, minDays, sentAfter);
            this.resultData.ColumnChanged += this.OnResultColumnChanged;

            this.gridBoard.FilterValueSource = this.resultData;
            this.RefreshViewRows();

            this.boardCard.TitleRightText =
                    "Days as of " + daysBasis.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            this.BindBoard(silent);
            this.RefreshSummary();

            if (keyword.Length == 0 && statusFilter.Count == 0
                    && sendFilter.Count == 0 && minDays == 0 && !sentAfter.HasValue
                    && board.Columns.Contains(ServerFields.Lot.LotId))
            {
                this.cboLotId.DataSource = board.DefaultView.ToTable(false, ServerFields.Lot.LotId);
                this.cboLotId.Text = string.Empty;
            }

            if (!string.IsNullOrEmpty(focusLotId))
            {
                this.FocusBoardRow(focusLotId);
            }
        }

        private void BindBoard(bool silent = false)
        {
            if (this.resultData == null || this.viewRows == null)
            {
                this.gridBoard.DataSource = null;
                this.gridBoard.StatusText = string.Empty;
                return;
            }

            DataTable view = this.resultData.Clone();

            for (int index = 0; index < this.viewRows.Count; index++)
            {
                view.ImportRow(this.viewRows[index]);
            }

            view.ColumnChanged += this.OnBoardColumnChanged;

            this.boardSelectionSilent = silent;
            try
            {
                this.ConfigureBoardColumns(view);
                this.gridBoard.DataSource = view;
            }
            finally
            {
                this.boardSelectionSilent = false;
            }
            this.UpdateBoardStatus();
        }

        private void ConfigureBoardColumns(DataTable view)
        {
            string schema = string.Join(",", TableJudgment.ColumnNames(view).ToArray());

            if (schema == this.boardColumnSchema)
            {
                return;
            }

            this.boardColumnSchema = schema;
            this.gridBoard.ConfigureColumns(BoardColumns(view));
        }

        private void UpdateBoardStatus()
        {
            if (this.resultData == null || this.viewRows == null)
            {
                this.gridBoard.StatusText = string.Empty;
                return;
            }

            int total = this.resultData.Rows.Count;
            int shown = this.viewRows.Count;
            int checkedCount = 0;

            foreach (DataRow row in this.resultData.Rows)
            {
                if (TableHelper.FlagSet(row, LogisticsRequestPresenter.CheckColumn))
                {
                    checkedCount = checkedCount + 1;
                }
            }

            string text = string.Empty;

            if (shown < total)
            {
                text = "filtered from " + total.ToString("N0", CultureInfo.InvariantCulture);
            }

            if (checkedCount > 0)
            {
                text = text.Length > 0
                        ? text + "  ·  " + checkedCount.ToString("N0", CultureInfo.InvariantCulture)
                                + " selected"
                        : checkedCount.ToString("N0", CultureInfo.InvariantCulture) + " selected";
            }

            this.gridBoard.StatusText = text;
        }

        private void RefreshViewRows()
        {
            this.viewRows = new List<DataRow>();

            if (this.resultData == null)
            {
                return;
            }

            foreach (DataRow row in this.resultData.Rows)
            {
                if (this.gridBoard.MatchesColumnFilters(row))
                {
                    this.viewRows.Add(row);
                }
            }
        }

        private void FocusBoardRow(string lotId)
        {
            if (this.viewRows == null)
            {
                return;
            }

            for (int index = 0; index < this.viewRows.Count; index++)
            {
                if (string.Equals(
                        TableHelper.CellText(this.viewRows[index], ServerFields.Lot.LotId),
                        lotId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    this.gridBoard.SelectedIndex = index;
                    return;
                }
            }
        }

        private void OnBoardColumnFiltersChanged(object sender, EventArgs e)
        {
            this.RefreshViewRows();

            this.BindBoard();
        }

        private void OnBoardColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            if (e.Column.ColumnName != LogisticsRequestPresenter.CheckColumn)
            {
                return;
            }

            DataRow source = this.FindResultRow(TableHelper.CellText(e.Row, ServerFields.Lot.LotId));

            if (source != null)
            {
                source[LogisticsRequestPresenter.CheckColumn] = e.ProposedValue;
            }
        }

        private DataRow FindResultRow(string lotId)
        {
            if (this.resultData == null || lotId.Length == 0)
            {
                return null;
            }

            foreach (DataRow row in this.resultData.Rows)
            {
                if (TableHelper.CellText(row, ServerFields.Lot.LotId) == lotId)
                {
                    return row;
                }
            }

            return null;
        }

        private void RefreshSummary()
        {
            LogisticsRequestPresenter.LogisticsSummary summary =
                    LogisticsRequestPresenter.Aggregate(this.resultData);

            this.badgeSent.Text = "Sent "
                    + summary.StatusCounts[LogisticsRequestPresenter.StatusSent].ToString("N0");
            this.badgeReceived.Text = "Received "
                    + summary.StatusCounts[LogisticsRequestPresenter.StatusReceived].ToString("N0");
            this.badgeUnlinked.Text = "Unlinked "
                    + summary.StatusCounts[LogisticsRequestPresenter.StatusUnlinked].ToString("N0");
            this.badgeLinked.Text = "Linked "
                    + summary.StatusCounts[LogisticsRequestPresenter.StatusLinked].ToString("N0");
            this.badgeCompleted.Text = "Completed "
                    + summary.StatusCounts[LogisticsRequestPresenter.StatusCompleted].ToString("N0");
            this.badgeUnmatched.Text = "Unmatched " + summary.UnmatchedCount.ToString("N0");

            this.badgeAvg.Text = summary.UnlinkedCount > 0
                    ? "Avg " + summary.DaysAverage.ToString("0.0", CultureInfo.InvariantCulture) + " d"
                    : "Avg -";
            this.badgeOldest.Text = summary.UnlinkedCount > 0
                    ? "Oldest " + summary.DaysMax.ToString("N0") + " d"
                    : "Oldest -";

            this.UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            int receiveCount = 0;
            int createCount = 0;

            if (this.resultData != null)
            {
                foreach (DataRow row in this.resultData.Rows)
                {
                    if (!TableHelper.FlagSet(row, LogisticsRequestPresenter.CheckColumn))
                    {
                        continue;
                    }

                    if (TableHelper.FlagSet(row, LogisticsRequestPresenter.ReceiveCanColumn))
                    {
                        receiveCount = receiveCount + 1;
                    }

                    if (TableHelper.FlagSet(row, LogisticsRequestPresenter.CreateCanColumn))
                    {
                        createCount = createCount + 1;
                    }
                }
            }

            this.btnReceive.Enabled = receiveCount > 0;
            this.btnReceive.BadgeCount = receiveCount;
            this.btnCreate.Enabled = createCount > 0;
            this.btnCreate.BadgeCount = createCount;
            this.btnManualReceive.Enabled = true;
        }

        private void OnResultColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            if (e.Column != null && e.Column.ColumnName == LogisticsRequestPresenter.CheckColumn)
            {
                this.Debounce("boardActions", 50, new MethodInvoker(this.UpdateActionButtons));
            }
        }

        private void OnBoardSelectionChanged(object sender, EventArgs e)
        {
            DataRowView row = this.gridBoard.SelectedItem as DataRowView;

            if (row == null)
            {
                this.stepPipeline.DataSource = null;
                this.gridWafers.DataSource = null;
                this.waferCard.Text = "Wafers";
                return;
            }

            this.UpdatePipeline(row.Row);

            string lotId = TableHelper.CellText(row.Row, ServerFields.Lot.LotId);

            if (string.IsNullOrEmpty(lotId))
            {
                return;
            }

            this.LoadWafers(lotId, this.boardSelectionSilent);
        }

        private void UpdatePipeline(DataRow row)
        {
            int status = Array.IndexOf(
                    LogisticsRequestPresenter.StatusNames, TableHelper.CellText(row, LogisticsRequestPresenter.StatusColumn));

            if (status < 0)
            {
                this.stepPipeline.DataSource = null;
                return;
            }

            bool notified = TableHelper.CellText(row, ServerFields.Pending.SendYn).Trim() == "Y";

            DataTable steps = new DataTable();
            steps.Columns.Add("LABEL", typeof(string));
            steps.Columns.Add("STATE", typeof(string));

            for (int index = 0; index < LogisticsRequestPresenter.StatusNames.Length; index++)
            {
                if (index == LogisticsRequestPresenter.StatusSent && !notified)
                {
                    steps.Rows.Add(LogisticsRequestPresenter.StatusNames[index], "Pending");
                    continue;
                }

                string state;

                if (index < status)
                {
                    state = "Completed";
                }
                else if (index == status)
                {
                    state = "Current";
                }
                else
                {
                    state = "Pending";
                }

                steps.Rows.Add(
                        LogisticsRequestPresenter.StatusNames[index].Replace(' ', '\n'), state);
            }

            this.stepPipeline.DataSource = steps;
        }

        private async void LoadWafers(string lotId, bool silent)
        {
            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelWafers, () => this.RequestWafers(lotId), silent);

            if (!outcome.IsCurrent)
            {
                return;
            }

            if (outcome.Failure != null)
            {
                this.gridWafers.DataSource = null;
                this.waferCard.Text = "Wafers";
                return;
            }

            WaferColumns(outcome.Value).Bind(this.gridWafers);
            this.waferCard.Text = "Wafers — " + lotId;
        }

        private DataTable BuildCheckedList(string actionFlag)
        {
            DataTable list = this.resultData != null ? this.resultData.Clone() : new DataTable();

            if (this.resultData != null)
            {
                foreach (DataRow row in this.resultData.Rows)
                {
                    if (TableHelper.FlagSet(row, LogisticsRequestPresenter.CheckColumn)
                            && TableHelper.FlagSet(row, actionFlag))
                    {
                        list.ImportRow(row);
                    }
                }
            }

            return list;
        }

        private void OnReceiveClick(object sender, EventArgs e)
        {
            DataTable receiveList = this.BuildCheckedList(LogisticsRequestPresenter.ReceiveCanColumn);

            if (receiveList.Rows.Count == 0)
            {
                this.ShowWarningMessage("Check lots to receive first.");
                return;
            }

            using (ReceiveDialogForm dialog = new ReceiveDialogForm(receiveList))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                this.ProcessBoardLots(CollectLotIds(receiveList), "Receive");
            }
        }

        private void OnManualReceiveClick(object sender, EventArgs e)
        {
            using (ManualReceiveDialogForm dialog = new ManualReceiveDialogForm())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                this.ShowToast(
                        dialog.ReceivedMessage.Length > 0
                                ? dialog.ReceivedMessage
                                : "Lot " + dialog.ReceivedLotId + " received.",
                        ToastKind.Success);
                this.ExecuteSearch(dialog.ReceivedLotId);
            }
        }

        private void OnCreateClick(object sender, EventArgs e)
        {
            DataTable createList = this.BuildCheckedList(LogisticsRequestPresenter.CreateCanColumn);

            if (createList.Rows.Count == 0)
            {
                this.ShowWarningMessage("Check lots to create first.");
                return;
            }

            using (CreateDialogForm dialog = new CreateDialogForm(createList))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                this.ProcessBoardLots(CollectLotIds(createList), "Create");
            }
        }

        private void OnGridCellButtonClick(object sender, GridButtonClickEventArgs e)
        {
            if (e.DataPropertyName != LogisticsRequestPresenter.ActionColumn)
            {
                return;
            }

            DataRowView row = e.Item as DataRowView;

            if (row == null)
            {
                return;
            }

            string step = LogisticsRequestPresenter.NextStep(row.Row);

            if (step.Length == 0 || !CanProcessRow(row.Row, step))
            {
                return;
            }

            List<string> single = new List<string>();
            single.Add(TableHelper.CellText(row.Row, ServerFields.Lot.LotId));

            this.ProcessBoardLots(single, step);
        }

        protected override void ShowRequestInfoDialog(DataTable requestInfo)
        {
            using (RequestInfoDialogForm dialog = new RequestInfoDialogForm())
            {
                dialog.SetRequest(requestInfo, RequestSpecimenFirstColumn, RequestPurposeColumn);
                dialog.ShowDialog(this);
            }
        }

        private void OnBoardMenuOpening(object sender, CancelEventArgs e)
        {
            DataRowView row = this.gridBoard.SelectedItem as DataRowView;

            if (row == null)
            {
                e.Cancel = true;
                return;
            }

            this.miAccept.Enabled = TableHelper.FlagSet(row.Row, LogisticsRequestPresenter.AcceptCanColumn);
            this.miReceive.Enabled = TableHelper.FlagSet(row.Row, LogisticsRequestPresenter.ReceiveCanColumn);
            this.miCreate.Enabled = TableHelper.FlagSet(row.Row, LogisticsRequestPresenter.CreateCanColumn);
        }

        private void OnMenuAcceptClick(object sender, EventArgs e)
        {
            this.ProcessSelectedBoardRow("Accept");
        }

        private void OnMenuReceiveClick(object sender, EventArgs e)
        {
            this.ProcessSelectedBoardRow("Receive");
        }

        private void OnMenuCreateClick(object sender, EventArgs e)
        {
            this.ProcessSelectedBoardRow("Create");
        }

        private void ProcessSelectedBoardRow(string step)
        {
            DataRowView row = this.gridBoard.SelectedItem as DataRowView;

            if (row == null || !CanProcessRow(row.Row, step))
            {
                return;
            }

            string lotId = TableHelper.CellText(row.Row, ServerFields.Lot.LotId);
            List<string> single = new List<string>();
            single.Add(lotId);

            this.ProcessBoardLots(single, step);
        }

        private void ProcessBoardLots(List<string> lotIds, string step)
        {
            if (lotIds == null || this.ActionInProgress)
            {
                return;
            }
            List<string> keys = lotIds.FindAll(id => !string.IsNullOrWhiteSpace(id));
            if (keys.Count == 0)
            {
                return;
            }
            string lastServerMessage = string.Empty;

            this.RunActionBatch(
                    keys,
                    delegate(string lotId)
                    {
                        if (step == "Accept")
                        {
                            return this.Accept(lotId);
                        }

                        return step == "Receive" ? this.Receive(lotId) : this.Create(lotId);
                    },
                    delegate(BatchProgress progress)
                    {
                        if (progress.Total > 1)
                        {
                            this.gridBoard.StatusText = "processing " + progress.Text;
                        }

                        if (progress.Last != null && progress.Last.Success
                                && progress.Last.Message.Length > 0)
                        {
                            lastServerMessage = progress.Last.Message;
                        }
                    },
                    delegate(BatchOutcome outcome)
                    {
                        if (outcome.Succeeded == 0)
                        {
                            string reason = outcome.FirstFailure == null
                                            || outcome.FirstFailure.Message.Length == 0
                                    ? "Nothing was processed."
                                    : outcome.FirstFailure.Message;

                            this.ShowErrorMessage(step + " failed", reason);
                            this.UpdateBoardStatus();
                            return;
                        }

                        string doneMessage = outcome.Total == 1 && lastServerMessage.Length > 0
                                ? lastServerMessage
                                : outcome.Succeeded.ToString("N0") + " lot(s) "
                                        + DoneWord(step)
                                        + (outcome.Failed > 0
                                                ? " " + outcome.Failed.ToString("N0")
                                                        + " failed."
                                                : string.Empty);

                        if (outcome.Failed > 0)
                        {
                            this.ShowWarningMessage(doneMessage);
                        }
                        else
                        {
                            this.ShowToast(doneMessage, ToastKind.Success);
                        }

                        this.ExecuteSearch(string.Empty, true);
                    },
                    step + "ing…");
        }

        private static string DoneWord(string step)
        {
            if (step == "Accept")
            {
                return "accepted.";
            }

            return step == "Receive" ? "received." : "created.";
        }

        private static List<string> CollectLotIds(DataTable checkedList)
        {
            List<string> lotIds = new List<string>();

            foreach (DataRow row in checkedList.Rows)
            {
                string lotId = TableHelper.CellText(row, ServerFields.Lot.LotId).Trim();

                if (lotId.Length > 0)
                {
                    lotIds.Add(lotId);
                }
            }

            return lotIds;
        }

        private void OnExportClick(object sender, EventArgs e)
        {
            this.ExportToExcel(
                    this.gridBoard, "Logistics & Request", this.resultData, "LogisticsRequest", "lots");
        }
    }
}
