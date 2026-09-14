using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.Controls.Wpf.Display;
using Modern.Lab.Controls.Wpf.Input;
using Modern.Lab.Data;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.Samples.Contracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.WinForms.Controls.Display;

namespace Modern.Lab.Samples
{

    public partial class LotManagementForm : ModernFormBase
    {
        private DataTable lotData;
        private static readonly ResponseContractSet Contracts = LotContracts.Build();
        private TableResponse lotCurrent;
        private TableResponse lotReserved;
        private TableResponse unitCurrent;
        private TableResponse unitReserved;
        private bool unitBinding;
        private readonly NewItemTracker newItems = new NewItemTracker();
        private string newItemCriteria;
        private DataTable unitData;
        private DataTable lotHistoryData;
        private DataTable unitHistoryData;
        private string unitHistoryOwner;

        private const string channelTypes = "types";
        private const string channelLots = "lots";
        private const string channelSuggest = "suggest";
        private const string channelUnits = "units";
        private const string channelLotHistory = "lotHistory";
        private const string channelUnitHistory = "unitHistory";
        private const string channelTrays = "trays";

        private static readonly ResponseContractSet CandidateContracts = LotCandidateContracts.Build();

        private TableResponse candidateTrays;
        private const int unitSelectionDelay = 300;
        private const string lotHistoryTitle = "Lot History";

        private const string userId = "operator";

        private const double badgeMesWidth = 100d;

        private bool typeSetup;
        private bool gridBinding;

        private bool searchReady;
        private string unitType = string.Empty;

        public LotManagementForm()
        {
            this.InitializeComponent();

            this.InitializeModernForm(this.midPanel);

            this.DeferredResize = true;

            this.RegisterFindShortcut(this.gridLotHistory, this.gridUnitHistory);

            this.menuLot.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
        }

        protected override void ShowRequestInfoDialog(DataTable requestInfo)
        {
            using (RequestInfoDialogForm dialog = new RequestInfoDialogForm())
            {
                dialog.SetRequest(requestInfo, RequestSpecimenFirstColumn, RequestDetailsColumn);
                dialog.ShowDialog(this);
            }
        }


        private DataTable RequestTypes()
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetLotTypeList");
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestLots(string types, string fromDate, string toDate, string lotId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetLotList");
            request.Append(" LOT_TYPE=").Append(types);
            request.Append(" FROM_DT=").Append(fromDate);
            request.Append(" TO_DT=").Append(toDate);
            request.Append(" LOT_ID=").Append(lotId);
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestLotIdCandidates(string keyword)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetLotIdCandidates");
            request.Append(" KEYWORD=").Append(keyword);
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestUnits(string lotId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetLotUnitList");
            request.Append(" LOT_ID=").Append(lotId);
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestLotHistory(string lotId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetLotHistory");
            request.Append(" LOT_ID=").Append(lotId);
            return this.Request(request.ToString()).Table;
        }


        private DataTable RequestTrays()
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetDurableList");
            request.Append(" DURABLE_TYPE=").Append(LotManagementPresenter.DurableTypeTray);
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestUnitHistory(string lotId, string wfId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetLotUnitHistory");
            request.Append(" LOT_ID=").Append(lotId);
            request.Append(" WF_ID=").Append(wfId);
            return this.Request(request.ToString()).Table;
        }



        private DataActionResult CreateChildLot(string childType, string parentLotId, string trayId)
        {
            StringBuilder request = new StringBuilder();
            request.Append(childType == ServerFields.Lot.SubProdTyp.Chip ? "CreateChipLot" : "CreateLamellaLot");
            request.Append(" PARENT_LOT_ID=").Append(parentLotId);
            request.Append(" TRAY_ID=").Append(trayId);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }


        private DataActionResult ChangeLotSpec(string lotId, string progId, string flowId, string operId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("ChangeLotSpec");
            request.Append(" LOT_ID=").Append(lotId);
            request.Append(" PROD_ID=").Append(progId);
            request.Append(" FLOW_ID=").Append(flowId);
            request.Append(" OPER_ID=").Append(operId);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }


        private DataActionResult HoldLot(string lotId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("HoldLot");
            request.Append(" LOT_ID=").Append(lotId);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }


        private DataActionResult ReleaseLot(string lotId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("ReleaseLot");
            request.Append(" LOT_ID=").Append(lotId);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }


        private DataActionResult ScrapLot(string lotId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("ScrapLot");
            request.Append(" LOT_ID=").Append(lotId);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            this.cboType.DisplayMember = "TYPE_NM";
            this.cboType.ValueMember = "TYPE_ID";

            DateTime today = DateTime.Today;
            this.dtpCreate.SetRange(today.AddMonths(-1), today);

            this.txtLotId.AutoCompleteMode = AutoCompleteMode.Suggest;
            this.txtLotId.AutoCompleteSource = AutoCompleteSource.CustomSource;
            this.txtLotId.SuggestionFilterMode = Modern.Lab.Controls.Wpf.Input.SuggestionFilterMode.None;

            this.gridLots.RowKeyMember = ServerFields.Lot.LotId;
            this.gridLots.CellLinkClick += this.OnReqSerialNoLinkClick;
            this.fieldInfo.FieldLinkClick += this.OnInfoLinkClick;
            this.gridUnitHistory.CellLinkClick += this.OnReqSerialNoLinkClick;

            this.gridUnits.RowKeyMember = ServerFields.Unit.WfId;
            this.ApplyUnitType(ServerFields.Lot.SubProdTyp.Wafer);

            this.fieldInfo.Columns = 5;

            this.gridLotHistory.RowKeyMember = "LOT_ID,TIMEKEY";
            this.gridUnitHistory.RowKeyMember = "WF_ID,TIMEKEY";
            this.gridUnitHistory.RowForegroundMember = JudgeResult.ColorColumn;

            this.PopulateMenu();

            this.ddbCreate.DisplayMember = "LABEL";
            this.ddbCreate.ValueMember = "VALUE";
            this.ddbCreate.EnabledMember = "CAN";
            this.ddbHold.DisplayMember = "LABEL";
            this.ddbHold.ValueMember = "VALUE";
            this.ddbHold.EnabledMember = "CAN";
            this.ddbExecute.DisplayMember = "LABEL";
            this.ddbExecute.ValueMember = "VALUE";
            this.ddbExecute.EnabledMember = "CAN";

            this.RefreshActionStates();

            this.LoadTypes();
        }

        private async void LoadTypes()
        {
            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelTypes, () => this.RequestTypes());

            if (!outcome.IsCurrent || outcome.Failure != null)
            {
                return;
            }

            this.typeSetup = true;

            try
            {
                this.cboType.DataSource = outcome.Value;
                this.cboType.CheckAll();
            }
            finally
            {
                this.typeSetup = false;
            }

            this.searchReady = true;
            this.ExecuteSearch();
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            if (!this.searchReady)
            {
                return;
            }

            this.CancelDebounce(channelSuggest);
            this.InvalidateChannel(channelSuggest);
            this.txtLotId.CloseSuggestions();
            this.ExecuteSearch();
        }

        private void OnLotIdTextChanged(object sender, EventArgs e)
        {
            if (!this.searchReady)
            {
                return;
            }

            if (this.IsExistingCandidate(this.txtLotId.Text.Trim()))
            {
                this.CancelDebounce(channelSuggest);
                return;
            }

            this.Debounce(channelSuggest, 300, new MethodInvoker(this.LoadIdCandidatesAsync));
        }

        private bool IsExistingCandidate(string text)
        {
            AutoCompleteStringCollection source = this.txtLotId.AutoCompleteCustomSource;

            if (source == null || text.Length == 0)
            {
                return false;
            }

            foreach (string candidate in source)
            {
                if (string.Equals(candidate, text, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private async void LoadIdCandidatesAsync()
        {
            string keyword = this.txtLotId.Text.Trim();

            if (keyword.Length == 0)
            {
                this.txtLotId.AutoCompleteCustomSource = null;
                return;
            }

            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelSuggest, () => this.RequestLotIdCandidates(keyword));

            if (!outcome.IsCurrent || outcome.Failure != null || outcome.Value == null)
            {
                return;
            }

            AutoCompleteStringCollection collection = new AutoCompleteStringCollection();

            foreach (DataRow row in outcome.Value.Rows)
            {
                collection.Add(TableHelper.CellText(row, ServerFields.Lot.LotId).Trim());
            }

            this.txtLotId.AutoCompleteCustomSource = collection;
        }

        private string SelectedTypes()
        {
            return LotManagementPresenter.TypeFilter(this.cboType.CheckedValues);
        }

        private string SelectedLotId()
        {
            DataRowView lot = this.gridLots.SelectedItem as DataRowView;

            return lot == null ? string.Empty : LotManagementPresenter.Id(lot.Row);
        }

        private async void ExecuteSearch(bool silent = false, string focusLotId = null)
        {
            string types = this.SelectedTypes();
            this.newItems.BeginQuery();
            this.RefreshActionStates();
            string fromDate = LotManagementPresenter.DateText(this.dtpCreate.FromDate);
            string toDate = LotManagementPresenter.DateText(this.dtpCreate.ToDate);
            string lotId = this.txtLotId.Text.Trim();
            string keepLotId = string.IsNullOrEmpty(focusLotId) ? this.SelectedLotId() : focusLotId;

            IDisposable busy = silent ? null : this.Busy("Loading lots...", fromDate + " ~ " + toDate);

            LoadOutcome<DataTable> outcome;

            try
            {
                outcome = await this.FetchAsync(
                        channelLots, () => this.RequestLots(types, fromDate, toDate, lotId), silent);
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
                this.RefreshActionStates();
                return;
            }

            this.newItemCriteria = NewItemTracker.Criteria(NewItemTracker.SelectionSet(types), fromDate, toDate, lotId);
            this.BindLots(outcome.Value, keepLotId, silent);
        }

        private void BindLots(DataTable lots, string keepLotId, bool silent)
        {
            DataTable canonical = this.BindJudged(LotContracts.LotTable, ref this.lotCurrent,
                    ref this.lotReserved, lots, LotManagementPresenter.ScreenColumns);
            bool judged = Judged(this.lotCurrent);
            this.lotData = NewItemTint.Apply(
                    this.newItems.Accept(canonical, ServerFields.Lot.LotId, this.newItemCriteria, judged), judged);
            this.gridLots.RowKeyMember = judged ? ServerFields.Lot.LotId : string.Empty;
            this.gridLots.RowColorMember = NewItemTint.Member(this.lotData);

            GridColumns columns = GridColumns.Of(this.lotData);
            if (judged)
            {
                columns
                    .Badge(ServerFields.Lot.MesProcStatCd.Column)
                    .BadgeWidth(ServerFields.Lot.MesProcStatCd.Column, ServerFields.Lot.MesProcStatCd.All)
                    .Link(ServerFields.Lot.ReqSerialNo);
                columns.Hide(NewItemTracker.ColumnName, NewItemTint.ColumnName);
                IssueBadge(columns, TableJudgment.HasInvalidRows(canonical));
            }
            if (judged)
            {
                columns.Apply(this.gridLots);
            }
            else
            {
                this.gridLots.ConfigureColumns(columns.ToArray());
            }

            FieldDefinitions.Of(judged ? this.lotCurrent.Table : canonical)
                    .Link(ServerFields.Lot.ReqSerialNo)
                    .Span("DESCRIPTION", 5)
                    .Apply(this.fieldInfo);

            string selectId = judged ? LotManagementPresenter.SelectionAfterBind(canonical, keepLotId) : string.Empty;

            this.gridBinding = true;

            try
            {
                this.gridLots.DataSource = this.lotData;
                this.gridLots.SelectedIndex = -1;
                this.SelectLot(selectId);
            }
            finally
            {
                this.gridBinding = false;
            }

            this.lotCard.TitleRightText = lots == null
                    ? string.Empty
                    : lots.Rows.Count.ToString("N0") + " lots";

            this.ApplyLotSelection(silent);
            this.RefreshContractNotice();
        }

        private void SelectLot(string lotId)
        {
            if (this.lotData == null || lotId.Length == 0)
            {
                return;
            }

            DataRowView first = null;
            foreach (DataRowView row in this.lotData.DefaultView)
            {
                if (TableJudgment.IsInvalid(row.Row) || LotManagementPresenter.Id(row.Row).Length == 0)
                {
                    continue;
                }
                if (first == null)
                {
                    first = row;
                }
                if (LotManagementPresenter.Id(row.Row) == lotId)
                {
                    this.gridLots.SelectedItem = row;
                    return;
                }
            }
            this.gridLots.SelectedItem = first;
        }

        private void OnLotSelectionChanged(object sender, EventArgs e)
        {
            if (this.gridBinding)
            {
                return;
            }

            this.ApplyLotSelection(false);
        }

        private void ApplyLotSelection(bool silent)
        {
            DataRowView lot = this.gridLots.SelectedItem as DataRowView;

            if (lot == null)
            {
                this.fieldInfo.ClearValues();
                this.ClearDependents();
                this.RefreshActionStates();
                return;
            }

            string lotId = LotManagementPresenter.Id(lot.Row);

            this.fieldInfo.SetRow(lot.Row);
            this.ApplyUnitType(LotManagementPresenter.Type(lot.Row));

            this.ClearDependents();
            this.RefreshActionStates();
            this.pageLotHistory.Text = lotHistoryTitle + " — " + lotId;
            this.ApplyUnitCardTitle();
            if (Judged(this.lotCurrent) && !TableJudgment.IsInvalid(lot.Row))
            {
                this.LoadDependents(lotId, silent);
            }
        }

        private void ApplyUnitType(string type)
        {
            if (this.unitType == type)
            {
                return;
            }

            this.unitType = type;
            this.unitCard.Text = LotManagementPresenter.UnitListTitle(type);
            this.ApplyUnitCardTitle();
            this.pageUnitHistory.Text = LotManagementPresenter.UnitHistoryTitle(type);
        }

        private void ApplyUnitCardTitle()
        {
            DataRowView lot = this.gridLots.SelectedItem as DataRowView;
            string title = LotManagementPresenter.UnitListTitle(this.unitType);
            this.unitCard.Text = lot == null ? title : title + " — " + LotManagementPresenter.Id(lot.Row);
        }

        private void ClearDependents()
        {
            this.InvalidateChannel(channelUnits);
            this.InvalidateChannel(channelLotHistory);
            this.InvalidateChannel(channelUnitHistory);

            this.unitData = null;
            this.unitCurrent = null;
            this.unitReserved = null;
            this.gridUnits.DataSource = null;
            this.lotHistoryData = null;
            this.gridLotHistory.DataSource = null;
            this.unitHistoryData = null;
            this.unitHistoryOwner = null;
            this.gridUnitHistory.DataSource = null;
            this.pageLotHistory.Text = lotHistoryTitle;
            this.unitCard.Text = LotManagementPresenter.UnitListTitle(this.unitType);
            this.pageUnitHistory.Text = LotManagementPresenter.UnitHistoryTitle(this.unitType);
            this.RefreshContractNotice();
        }

        private void LoadDependents(string lotId, bool silent)
        {
            this.LoadDependent(channelUnits, () => this.RequestUnits(lotId), table => this.BindUnits(table, silent), silent);
            this.LoadDependent(channelLotHistory, () => this.RequestLotHistory(lotId), this.BindLotHistory, silent);
        }

        private async void LoadDependent(string channel, Func<DataTable> request, Action<DataTable> bind, bool silent)
        {
            LoadOutcome<DataTable> outcome = await this.FetchAsync(channel, request, silent);

            if (!outcome.IsCurrent)
            {
                return;
            }

            bind(outcome.Failure != null ? null : outcome.Value);
        }

        private void BindUnits(DataTable units, bool silent)
        {
            this.unitData = this.BindJudged(LotContracts.UnitTable, ref this.unitCurrent,
                    ref this.unitReserved, units, LotManagementPresenter.UnitScreenColumns);
            bool judged = Judged(this.unitCurrent);
            this.gridUnits.RowKeyMember = judged ? ServerFields.Unit.WfId : string.Empty;
            GridColumns columns = GridColumns.Of(this.unitData);
            if (judged)
            {
                columns
                    .Caption(ServerFields.Unit.WfId, LotManagementPresenter.UnitWord(this.unitType) + " Id");
                IssueBadge(columns, TableJudgment.HasInvalidRows(this.unitData));
            }
            if (judged)
            {
                columns.Apply(this.gridUnits);
            }
            else
            {
                this.gridUnits.ConfigureColumns(columns.ToArray());
            }
            this.unitBinding = true;
            try
            {
                this.gridUnits.DataSource = this.unitData;
                this.gridUnits.SelectedIndex = -1;
                if (judged && this.unitData != null)
                {
                    foreach (DataRowView row in this.unitData.DefaultView)
                    {
                        if (!TableJudgment.IsInvalid(row.Row))
                        {
                            this.gridUnits.SelectedItem = row;
                            break;
                        }
                    }
                }
            }
            finally
            {
                this.unitBinding = false;
            }
            this.ApplyUnitSelection(silent);
            this.RefreshContractNotice();
        }

        private void BindLotHistory(DataTable history)
        {
            this.lotHistoryData = history;

            GridColumns.Of(history)
                    .Badge(ServerFields.Lot.MesProcStatCd.Column)
                    .BadgeWidth(ServerFields.Lot.MesProcStatCd.Column, ServerFields.Lot.MesProcStatCd.All)
                    .Bind(this.gridLotHistory);
            this.gridLotHistory.DataSource = history;
        }

        private void BindUnitHistory(DataTable history)
        {
            JudgeResult.Mark(history);
            this.unitHistoryData = history;

            GridColumns.Of(history)
                    .Caption(ServerFields.Unit.WfId, LotManagementPresenter.UnitWord(this.unitType) + " Id")
                    .Link(ServerFields.Lot.ReqSerialNo)
                    .Bind(this.gridUnitHistory);
        }

        private void OnUnitSelectionChanged(object sender, EventArgs e)
        {
            if (this.unitBinding)
            {
                return;
            }
            this.Debounce(channelUnitHistory, unitSelectionDelay, new MethodInvoker(delegate { this.ApplyUnitSelection(false); }));
        }

        private void ApplyUnitSelection(bool silent)
        {
            this.CancelDebounce(channelUnitHistory);

            DataRowView lot = this.gridLots.SelectedItem as DataRowView;
            DataRowView unit = this.gridUnits.SelectedItem as DataRowView;
            string unitId = unit == null ? string.Empty : TableHelper.CellText(unit.Row, ServerFields.Unit.WfId).Trim();

            if (lot == null || unitId.Length == 0 || !Judged(this.lotCurrent) || TableJudgment.IsInvalid(lot.Row)
                    || !Judged(this.unitCurrent) || TableJudgment.IsInvalid(unit.Row))
            {
                this.InvalidateChannel(channelUnitHistory);
                this.unitHistoryData = null;
                this.unitHistoryOwner = null;
                this.gridUnitHistory.DataSource = null;
                this.pageUnitHistory.Text = LotManagementPresenter.UnitHistoryTitle(this.unitType);
                return;
            }

            string lotId = LotManagementPresenter.Id(lot.Row);

            this.unitHistoryOwner = unitId;
            this.pageUnitHistory.Text = LotManagementPresenter.UnitHistoryTitle(this.unitType) + " — " + unitId;
            this.LoadDependent(
                    channelUnitHistory,
                    () => this.RequestUnitHistory(lotId, unitId),
                    this.BindUnitHistory, silent);
        }

        private void RefreshActionStates()
        {
            DataRowView lot = this.gridLots.SelectedItem as DataRowView;
            DataRow row = lot == null || !this.newItems.ActionsReady ? null : lot.Row;

            ActionGate gate = this.BuildGate(row);
            this.ddbCreate.DataSource = LotManagementPresenter.BuildGatedMenuTable(LotManagementPresenter.CreateActions, key => this.ActionEnabled(key, row, gate));
            this.ddbHold.DataSource = LotManagementPresenter.BuildGatedMenuTable(LotManagementPresenter.HoldActions, key => this.ActionEnabled(key, row, gate));
            this.ddbExecute.DataSource = LotManagementPresenter.BuildGatedMenuTable(LotManagementPresenter.ExecuteActions, key => this.ActionEnabled(key, row, gate));
            this.ddbHold.Enabled = this.newItems.ActionsReady && row != null;
            this.ddbExecute.Enabled = this.newItems.ActionsReady && row != null;

            this.lblTarget.Text = LotManagementPresenter.StatusLineText(row, gate);
        }

        private void OnActionClicked(object sender, DropDownItemClickedEventArgs e)
        {
            this.ExecuteAction(e.Value as string);
        }

        private void ExecuteAction(string key)
        {
            DataRowView lot = this.gridLots.SelectedItem as DataRowView;
            DataRow row = lot == null ? null : lot.Row;

            if (!LotManagementPresenter.IsActionKey(key) || !this.ActionEnabled(key, row, this.BuildGate(row))
                    || (key != LotManagementPresenter.ActionCreateWafer && !this.CanProcess(key, LotManagementPresenter.Id(row))))
            {
                return;
            }

            switch (key)
            {
                case LotManagementPresenter.ActionCreateWafer:
                    this.ShowCreateWaferDialog();
                    break;

                case LotManagementPresenter.ActionCreateChip:
                case LotManagementPresenter.ActionCreateLamella:
                    this.ExecuteCreateChild(row, LotManagementPresenter.ChildTypeOf(key));
                    break;

                case LotManagementPresenter.ActionHold:
                    this.ExecuteHold(row);
                    break;

                case LotManagementPresenter.ActionNotOnHold:
                    this.ExecuteRelease(row);
                    break;

                case LotManagementPresenter.ActionChangeSpec:
                    this.ShowChangeSpecDialog(row);
                    break;

                case LotManagementPresenter.ActionScrap:
                    this.ExecuteScrap(row);
                    break;
            }
        }

        private void OnInfoLinkClick(object sender, ModernFieldLinkClickEventArgs e)
        {
            if (e.Member == ReqSerialNoColumn)
            {
                this.ShowRequestInfo(e.Value);
            }
        }

        private void ShowCreateWaferDialog()
        {
            using (CreateWaferDialogForm dialog = new CreateWaferDialogForm())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                this.ShowToast(
                        dialog.CreatedMessage.Length > 0
                                ? dialog.CreatedMessage
                                : "Lot " + dialog.CreatedLotId + " created.",
                        ToastKind.Success);
                this.ExecuteSearch(true, dialog.CreatedLotId);
            }
        }

        private async void ExecuteCreateChild(DataRow lot, string childType)
        {
            string parentLotId = LotManagementPresenter.Id(lot);
            string key = childType == ServerFields.Lot.SubProdTyp.Chip ? LotManagementPresenter.ActionCreateChip : LotManagementPresenter.ActionCreateLamella;
            LoadOutcome<DataTable> outcome = await this.FetchAsync(channelTrays, () => this.RequestTrays());

            if (!outcome.IsCurrent || outcome.Failure != null || !this.CanProcess(key, parentLotId))
            {
                return;
            }

            this.candidateTrays = CandidateGate.Receive(CandidateContracts, LotCandidateContracts.TrayTable, outcome.Value);

            LotCreateChildDialogOptions options = new LotCreateChildDialogOptions();
            options.Title = "Create " + childType + " Lot — " + parentLotId;
            options.OkText = "Create";

            options.ValueReadOnly = childType;

            options.Targets = CandidateGate.Candidates(this.candidateTrays);
            options.TargetIssue = CandidateGate.ShapeIssue(this.candidateTrays, "Tray list");
            options.TargetDisplayMember = LotManagementPresenter.DurableIdColumn;
            options.TargetValueMember = LotManagementPresenter.DurableIdColumn;

            this.OpenCreateChildDialog(parentLotId, childType, options);
        }

        protected virtual void OpenCreateChildDialog(
                string parentLotId, string childType, LotCreateChildDialogOptions options)
        {
            using (LotCreateChildDialogForm dialog = new LotCreateChildDialogForm(options))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.ProcessCreateChild(childType, parentLotId, dialog.TrayId);
                }
            }
        }

        private void ExecuteHold(DataRow lot)
        {
            string lotId = LotManagementPresenter.Id(lot);

            if (!this.Confirm(
                    "Hold lot " + Modern.Lab.WinForms.Controls.Dialogs.ModernMessageDialog.Emphasis(lotId) + "?",
                    "Confirm"))
            {
                return;
            }

            this.ProcessHold(lotId);
        }

        private void ExecuteRelease(DataRow lot)
        {
            string lotId = LotManagementPresenter.Id(lot);

            if (!this.Confirm(
                    "Release lot " + Modern.Lab.WinForms.Controls.Dialogs.ModernMessageDialog.Emphasis(lotId)
                            + " from hold?",
                    "Confirm"))
            {
                return;
            }

            this.ProcessRelease(lotId);
        }

        private void ShowChangeSpecDialog(DataRow lot)
        {
            string lotId = LotManagementPresenter.Id(lot);
            using (ChangeSpecDialogForm dialog = new ChangeSpecDialogForm(lot))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.ProcessChangeSpec(lotId, dialog.ProdId, dialog.FlowId, dialog.OperId);
                }
            }
        }

        private void ExecuteScrap(DataRow lot)
        {
            string lotId = LotManagementPresenter.Id(lot);

            if (!this.Confirm(
                    "Scrap lot " + Modern.Lab.WinForms.Controls.Dialogs.ModernMessageDialog.Emphasis(lotId)
                            + "? This cannot be undone.",
                    "Confirm"))
            {
                return;
            }

            this.ProcessScrap(lotId);
        }


        private void ProcessCreateChild(string childType, string parentLotId, string trayId)
        {
            string key = childType == ServerFields.Lot.SubProdTyp.Chip ? LotManagementPresenter.ActionCreateChip : LotManagementPresenter.ActionCreateLamella;

            if (this.candidateTrays == null)
            {
                this.ShowToast("Create " + childType + ": Tray candidates not loaded — open the dialog again.", ToastKind.Warning);
                return;
            }

            ActionGate gate = new ActionGate(CandidateContracts).Observe(
                    LotCandidateContracts.TrayTable, this.candidateTrays,
                    CandidateGate.Find(this.candidateTrays, LotManagementPresenter.DurableIdColumn, trayId));

            if (!gate.CanExecute(key))
            {
                this.ShowToast("Create " + childType + ": " + CandidateGate.ReasonText(gate, key, null), ToastKind.Warning);
                return;
            }

            this.Process(
                    key, parentLotId,
                    () => this.CreateChildLot(childType, parentLotId, trayId),
                    "Creating " + childType + " lot from " + parentLotId + "…",
                    reply => this.ExecuteSearch(true));
        }

        private void ProcessChangeSpec(string lotId, string progId, string flowId, string operId)
        {
            if (string.IsNullOrWhiteSpace(progId) || string.IsNullOrWhiteSpace(flowId) || string.IsNullOrWhiteSpace(operId))
            {
                this.ShowToast("Change Spec: select Prog, Flow and Oper.", ToastKind.Warning);
                return;
            }

            this.Process(
                    LotManagementPresenter.ActionChangeSpec, lotId,
                    () => this.ChangeLotSpec(lotId, progId, flowId, operId),
                    "Changing spec of " + lotId + "…",
                    reply => this.ExecuteSearch(true));
        }

        private void ProcessHold(string lotId)
        {
            this.Process(
                    LotManagementPresenter.ActionHold, lotId,
                    () => this.HoldLot(lotId),
                    "Holding " + lotId + "…",
                    reply => this.ExecuteSearch(true));
        }

        private void ProcessRelease(string lotId)
        {
            this.Process(
                    LotManagementPresenter.ActionNotOnHold, lotId,
                    () => this.ReleaseLot(lotId),
                    "Releasing " + lotId + "…",
                    reply => this.ExecuteSearch(true));
        }

        private void ProcessScrap(string lotId)
        {
            this.Process(
                    LotManagementPresenter.ActionScrap, lotId,
                    () => this.ScrapLot(lotId),
                    "Scrapping " + lotId + "…",
                    reply => this.ExecuteSearch(true));
        }

        private void Process(string key, string lotId, Func<DataActionResult> call, string busyText, Action<DataActionResult> refresh)
        {
            if (!this.CanProcess(key, lotId))
            {
                return;
            }

            this.RunAction(
                    call,
                    delegate(DataActionResult reply)
                    {
                        this.ShowToast(
                                reply.Message.Length > 0 ? reply.Message : "Done.",
                                ToastKind.Success);
                        refresh(reply);
                    },
                    busyText);
        }

        private void PopulateMenu()
        {
            this.menuLot.Items.Clear();
            this.AppendMenuSection("Create", LotManagementPresenter.CreateActions);
            this.AppendMenuSection("Hold/Release", LotManagementPresenter.HoldActions);
            this.AppendMenuSection("Execute", LotManagementPresenter.ExecuteActions);
        }

        private void AppendMenuSection(string headerText, IList<LotAction> actions)
        {
            this.menuLot.Items.Add(new ToolStripLabel(headerText));

            foreach (LotAction action in actions)
            {
                this.menuLot.Items.Add(this.CreateMenuItem(action));
            }
        }

        private ToolStripMenuItem CreateMenuItem(LotAction action)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(action.Label);
            item.Tag = action.Key;
            item.Click += this.OnLotMenuItemClick;
            return item;
        }

        private void OnMenuLotOpening(object sender, CancelEventArgs e)
        {
            DataRowView lot = this.gridLots.SelectedItem as DataRowView;
            DataRow row = lot == null ? null : lot.Row;
            ActionGate gate = this.BuildGate(row);

            foreach (object entry in this.menuLot.Items)
            {
                ToolStripMenuItem item = entry as ToolStripMenuItem;
                string key = item == null ? null : item.Tag as string;

                if (key != null && LotManagementPresenter.IsActionKey(key))
                {
                    item.Enabled = this.ActionEnabled(key, row, gate);
                    item.ToolTipText = LotManagementPresenter.ActionReasonText(gate, key);
                }
            }
        }

        private void OnLotMenuItemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item != null)
            {
                this.ExecuteAction(item.Tag as string);
            }
        }
    }
}
