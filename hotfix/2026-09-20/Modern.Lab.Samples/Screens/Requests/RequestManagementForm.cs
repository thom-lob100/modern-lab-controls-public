using System;
using System.Data;
using Modern.Lab.Data;
using Modern.Lab.Hosting;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.Samples.Contracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.Theming;
using Modern.Lab.WinForms.Controls.Data;

namespace Modern.Lab.Samples
{

    public partial class RequestManagementForm : ModernFormBase
    {
        private const string channelRequests = "requests";
        private const string channelDetails = "requestDetails";

        private static readonly ResponseContractSet Contracts = RequestContracts.Build();

        private DataTable requestData;
        private TableResponse requestCurrent;
        private TableResponse requestReserved;
        private int requestsInFlight;
        private bool gridBinding;
        private string detailKey = string.Empty;
        private string searchKey = string.Empty;
        private string nextStateCode = string.Empty;
        private bool submitting;

        public RequestManagementForm()
        {
            this.InitializeComponent();

            this.InitializeModernForm(this.mainZone);

            this.DeferredResize = true;

            this.RegisterFindShortcut(this.gridRequests, this.gridDetails);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            this.gridRequests.RowKeyMember = ServerFields.Request.ReqSerialNo;

            this.cboState.DisplayMember = RequestManagementPresenter.OptionLabelColumn;
            this.cboState.ValueMember = RequestManagementPresenter.OptionValueColumn;
            this.cboState.DataSource = RequestManagementPresenter.StateOptions();

            this.ClearStatus();
            this.ApplyDefaultRequestRange();
            this.UpdateSearchState();
            this.ExecuteSearch(false);
        }

        private void ApplyDefaultRequestRange()
        {
            DateTime today = DateTime.Today;
            this.dtpRequest.SetRange(RequestManagementPresenter.DefaultRequestFrom(today), today);
        }

        private void OnRequestRangeChanged(object sender, EventArgs e)
        {
            this.UpdateSearchState();
        }

        private bool RequestRangeComplete
        {
            get { return this.dtpRequest.FromDate.HasValue && this.dtpRequest.ToDate.HasValue; }
        }

        private void UpdateSearchState()
        {
            this.btnRefresh.Enabled = this.RequestRangeComplete;
            this.btnNextStep.Enabled = this.nextStateCode.Length > 0 && !this.submitting && this.RequestRangeComplete;
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            this.ExecuteSearch(false);
        }

        private void OnResetClick(object sender, EventArgs e)
        {
            if (this.submitting)
            {
                return;
            }

            this.txtReqSerialNo.Text = string.Empty;
            this.txtRequester.Text = string.Empty;
            this.cboState.UncheckAll();
            this.ApplyDefaultRequestRange();
            this.dtpAccept.SetRange(null, null);
            this.ExecuteSearch(false);
        }

        private SearchConditions ReadConditions()
        {
            SearchConditions conditions = new SearchConditions();
            conditions.ReqSerialNo = this.txtReqSerialNo.Text.Trim();
            conditions.Requester = this.txtRequester.Text.Trim();
            conditions.StateCodes = RequestManagementPresenter.StateFilter(this.cboState.CheckedValues);
            conditions.ReqFromDt = RequestManagementPresenter.DateText(this.dtpRequest.FromDate);
            conditions.ReqToDt = RequestManagementPresenter.DateText(this.dtpRequest.ToDate);
            conditions.AcceptFromDt = RequestManagementPresenter.DateText(this.dtpAccept.FromDate);
            conditions.AcceptToDt = RequestManagementPresenter.DateText(this.dtpAccept.ToDate);

            return conditions;
        }

        private async void ExecuteSearch(bool silent)
        {
            if (this.submitting || !this.RequestRangeComplete)
            {
                return;
            }

            SearchConditions conditions = this.ReadConditions();
            string key = conditions.Key;
            string keepKey = key == this.searchKey ? this.SelectedRequestId() : string.Empty;

            this.InvalidateChannel(channelDetails);
            this.requestsInFlight++;

            IDisposable busy = silent
                    ? null
                    : this.Busy(
                            "Loading requests...",
                            RequestManagementPresenter.FilteredRequestsText);

            LoadOutcome<DataTable> outcome;

            try
            {
                outcome = await this.FetchAsync(channelRequests, () => this.RequestRequests(conditions), silent);
            }
            finally
            {
                this.requestsInFlight--;

                if (busy != null)
                {
                    busy.Dispose();
                }
            }

            if (!outcome.IsCurrent)
            {
                return;
            }

            this.searchKey = key;
            this.BindRequests(outcome.Failure != null ? null : outcome.Value, keepKey, silent);
        }

        private void BindRequests(DataTable requests, string keepKey, bool silent)
        {
            this.requestData = this.BindJudged(
                    RequestContracts.RequestTable, ref this.requestCurrent, ref this.requestReserved, requests, silent);

            this.gridBinding = true;

            try
            {
                if (this.requestData == null)
                {
                    this.gridRequests.DataSource = null;
                }
                else
                {
                    bool judged = this.requestCurrent != null;
                    this.gridRequests.RowKeyMember = judged ? ServerFields.Request.ReqSerialNo : string.Empty;

                    GridColumns columns = GridColumns.Of(this.requestData);

                    if (judged)
                    {
                        columns
                            .Badge(ServerFields.Request.StateCode.Column)
                            .BadgeWidth(ServerFields.Request.StateCode.Column, ServerFields.Request.StateCode.All)
                            .Center(ServerFields.Request.Status);

                        if (!TableJudgment.HasInvalidRows(this.requestData))
                        {
                            columns.Hide(TableJudgment.IssueColumn);
                        }
                    }

                    columns.Apply(this.gridRequests);

                    this.gridRequests.DataSource = this.requestData;

                    this.SelectRequest(keepKey);
                }
            }
            finally
            {
                this.gridBinding = false;
            }

            this.ApplySelection(silent);
        }

        private void SelectRequest(string keepKey)
        {
            if (this.requestData == null || this.requestData.Rows.Count == 0)
            {
                this.gridRequests.SelectedIndex = -1;
                return;
            }

            if (keepKey.Length > 0)
            {
                foreach (DataRowView row in this.requestData.DefaultView)
                {
                    if (RequestId(row.Row) == keepKey)
                    {
                        this.gridRequests.SelectedItem = row;
                        return;
                    }
                }

                this.gridRequests.SelectedIndex = -1;
                return;
            }

            this.gridRequests.SelectedIndex = 0;
        }

        private void OnRequestSelectionChanged(object sender, EventArgs e)
        {
            if (this.gridBinding)
            {
                return;
            }

            if (this.requestsInFlight > 0 || this.submitting)
            {
                this.RestoreSelection();
                return;
            }

            string key = this.SelectedRequestId();

            if (key.Length > 0 && key == this.detailKey)
            {
                return;
            }

            this.ApplySelection(false);
        }

        private void RestoreSelection()
        {
            if (this.SelectedRequestId() == this.detailKey)
            {
                return;
            }

            this.gridBinding = true;

            try
            {
                if (this.detailKey.Length == 0)
                {
                    this.gridRequests.SelectedIndex = -1;
                }
                else
                {
                    this.SelectRequest(this.detailKey);
                }
            }
            finally
            {
                this.gridBinding = false;
            }
        }

        private void ApplySelection(bool silent)
        {
            DataRowView request = this.gridRequests.SelectedItem as DataRowView;

            if (request == null)
            {
                this.detailKey = string.Empty;
                this.ClearDependents();
                return;
            }

            string key = RequestId(request.Row);

            if (key.Length == 0)
            {
                this.detailKey = string.Empty;
                this.ClearDependents();
                return;
            }

            this.InvalidateChannel(channelDetails);
            this.detailKey = key;
            this.lblRequestKey.Text = key;
            this.ApplyNextStep(request.Row);
            this.ApplyStatus(this.StateOf(request.Row));

            this.LoadDetails(key, silent);
        }

        private async void LoadDetails(string reqSerialNo, bool silent)
        {
            IDisposable cover = this.Busy(RequestManagementPresenter.LoadingSamplesText, reqSerialNo, this.detailCard);
            LoadOutcome<DataTable> outcome;

            try
            {
                outcome = await this.FetchAsync(channelDetails, () => this.RequestDetails(reqSerialNo), silent);
            }
            finally
            {
                cover.Dispose();
            }

            if (!outcome.IsCurrent)
            {
                return;
            }

            this.BindDetails(outcome.Failure != null ? null : outcome.Value);
        }

        private void BindDetails(DataTable details)
        {
            this.gridDetails.DataSource = details;
        }

        private void ApplyStatus(string stateCode)
        {
            this.stepStatus.DataSource = RequestManagementPresenter.Steps(stateCode);

            string text = RequestManagementPresenter.StateText(stateCode);

            this.badgeState.Text = text;
            this.badgeState.Semantic = RequestManagementPresenter.StateSemantic(stateCode);
            this.badgeState.Visible = text.Length > 0;
        }

        private void ClearDependents()
        {
            this.InvalidateChannel(channelDetails);

            this.gridDetails.DataSource = null;

            this.ClearStatus();
        }

        private void ApplyNextStep(DataRow request)
        {
            this.nextStateCode = request == null
                    ? string.Empty
                    : RequestManagementPresenter.NextState(this.StateOf(request));

            this.btnNextStep.Text = RequestManagementPresenter.NextStepText(this.nextStateCode);
            this.btnNextStep.Enabled = this.nextStateCode.Length > 0 && !this.submitting && this.RequestRangeComplete;
        }

        private void OnNextStepClick(object sender, EventArgs e)
        {
            string reqSerialNo = this.detailKey;
            string next = this.nextStateCode;
            DataRowView request = this.gridRequests.SelectedItem as DataRowView;

            if (reqSerialNo.Length == 0 || next.Length == 0 || this.submitting || request == null)
            {
                return;
            }

            DataRow sentRow = request.Row;

            if (!this.Confirm(
                    RequestManagementPresenter.NextStepConfirm(reqSerialNo, next),
                    RequestManagementPresenter.NextStepCaption))
            {
                return;
            }

            this.submitting = true;
            this.btnNextStep.Enabled = false;

            this.RunAction(
                    () => this.SetRequestState(reqSerialNo, next, ServerFields.Request.StateCode.StatusOf(next)),
                    delegate(DataActionResult success)
                    {
                        this.submitting = false;
                        this.ExecuteSearch(false);
                    },
                    delegate(DataActionResult rejected)
                    {
                        this.submitting = false;
                        this.ApplyNextStep(sentRow);
                        this.ShowActionFailure(rejected);
                    },
                    RequestManagementPresenter.NextStepBusyText);
        }

        private void ClearStatus()
        {
            this.ApplyNextStep(null);

            this.stepStatus.DataSource = RequestManagementPresenter.Steps(string.Empty);

            this.badgeState.Text = string.Empty;
            this.badgeState.Semantic = SemanticKind.Neutral;
            this.badgeState.Visible = false;

            this.lblRequestKey.Text = RequestManagementPresenter.NoSelectionText;
        }

        private string SelectedRequestId()
        {
            DataRowView request = this.gridRequests.SelectedItem as DataRowView;

            return request == null ? string.Empty : RequestId(request.Row);
        }

        private string StateOf(DataRow request)
        {
            return request.Table.Columns.Contains(ServerFields.Request.StateCode.Column)
                    ? TableHelper.CellText(request, ServerFields.Request.StateCode.Column)
                    : string.Empty;
        }

        private static string RequestId(DataRow request)
        {
            return RequestManagementPresenter.Normalize(
                    TableHelper.CellText(request, ServerFields.Request.ReqSerialNo));
        }

        private DataTable BindJudged(
                string tableId, ref TableResponse current, ref TableResponse reserved, DataTable incoming, bool silent)
        {
            reserved = null;

            if (incoming == null)
            {
                current = null;
                return null;
            }

            TableReception reception = TableJudgment.Receive(
                    tableId, incoming, Contracts, RequestManagementPresenter.ScreenColumns);

            if (reception.IsMissingRequired)
            {
                current = null;
                this.ShowMissingColumns(reception.Response, silent);
                return null;
            }

            if (reception.HasReservedColumns)
            {
                current = null;
                reserved = reception.Response;
                return reception.DisplayCopy;
            }

            DataTable table = reception.Normalized;
            current = TableJudgment.Judge(reception, table, Contracts);
            return table;
        }

        private sealed class SearchConditions
        {
            public string ReqSerialNo { get; set; }
            public string Requester { get; set; }
            public string StateCodes { get; set; }
            public string ReqFromDt { get; set; }
            public string ReqToDt { get; set; }
            public string AcceptFromDt { get; set; }
            public string AcceptToDt { get; set; }

            public SearchConditions()
            {
                this.ReqSerialNo = string.Empty;
                this.Requester = string.Empty;
                this.StateCodes = string.Empty;
                this.ReqFromDt = string.Empty;
                this.ReqToDt = string.Empty;
                this.AcceptFromDt = string.Empty;
                this.AcceptToDt = string.Empty;
            }

            public string Key
            {
                get
                {
                    return string.Join(
                            "|",
                            this.ReqSerialNo, this.Requester, this.StateCodes,
                            this.ReqFromDt, this.ReqToDt, this.AcceptFromDt, this.AcceptToDt);
                }
            }

            public bool IsEmpty
            {
                get
                {
                    return this.ReqSerialNo.Length + this.Requester.Length + this.StateCodes.Length
                            + this.ReqFromDt.Length + this.ReqToDt.Length
                            + this.AcceptFromDt.Length + this.AcceptToDt.Length == 0;
                }
            }
        }
    }
}
