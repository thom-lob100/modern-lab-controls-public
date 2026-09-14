using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.Controls.Wpf.Display;
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

    public partial class DurableManagementForm : ModernFormBase
    {
        private DataTable durableData;
        private DataTable boundDurables;
        private readonly NewItemTracker newItems = new NewItemTracker();
        private int durableSearchVersion;
        private string newItemCriteria;
        private DataTable slotData;
        private DataTable historyData;
        private DataTable stubHistoryData;
        private DataTable lccHistoryData;

        private const string channelTypes = "types";
        private const string channelDurables = "durables";
        private const string channelSlots = "slots";
        private const string channelWaferList = "waferList";
        private const string channelChipList = "chipList";
        private const string channelLamellaList = "lamellaList";
        private const string channelHistory = "history";
        private const string channelStubHistory = "stubHistory";
        private const string channelLccHistory = "lccHistory";
        private const string channelLocations = "locations";
        private const string channelCategories = "categories";
        private const string channelEditContents = "editContents";
        private const string durableHistoryTitle = "Durable History";
        private const string stubHistoryTitle = "Stub History";
        private const string lccHistoryTitle = "Lcc History";

        private const string userId = "operator";

        private DataTable typeData;
        private bool typeSetup;
        private bool gridBinding;

        private string layoutType = string.Empty;

        private static readonly ResponseContractSet Contracts = DurableContracts.Build();

        private static readonly ResponseContractSet CandidateContracts = DurableCandidateContracts.Build();

        private TableResponse candidateTypes;
        private TableResponse candidateCategories;
        private TableResponse candidateTargets;
        private TableResponse candidateLocations;

        private TableResponse durableCurrent;
        private TableResponse durableReserved;

        private const double issueWidth = 84d;

        public DurableManagementForm()
        {
            this.InitializeComponent();

            this.InitializeModernForm(this.midPanel);

            this.DeferredResize = true;

            this.RegisterFindShortcut(this.gridHistory, this.gridStubHistory, this.gridLccHistory);

            this.menuDurable.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
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
            request.Append("GetDurableTypeList");
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestDurables(string durableType)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetDurableList");
            request.Append(" DURABLE_TYPE=").Append(durableType);
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestSlots(string durableId, string subType)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetDurableSlotList");
            request.Append(" DURABLE_ID=").Append(durableId);
            request.Append(" SUB_TYPE=").Append(subType);

            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestHistory(string durableId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetDurableHistory");
            request.Append(" DURABLE_ID=").Append(durableId);
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestUnitHistory(string durableId, string subType)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetUnitHistory");
            request.Append(" DURABLE_ID=").Append(durableId);
            request.Append(" SUB_TYPE=").Append(subType);
            return this.Request(request.ToString()).Table;
        }


        private DataTable RequestCategories()
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetDurableCategoryList");
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestLocations()
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetLocationList");
            return this.Request(request.ToString()).Table;
        }

        protected virtual string TransportLocationId(DataRow durable)
        {
            return DurableManagementPresenter.Location(durable);
        }



        private DataActionResult CreateDurable(string durableType, string category, string description)
        {
            StringBuilder request = new StringBuilder();
            request.Append("CreateDurable");
            request.Append(" DURABLE_TYPE=").Append(durableType);
            request.Append(" CATEGORY=").Append(category);
            request.Append(" DESCRIPTION=").Append(description);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }


        private DataActionResult SetRfId(string durableId, string rfid)
        {
            StringBuilder request = new StringBuilder();
            request.Append("SetDurableRfId");
            request.Append(" DURABLE_ID=").Append(durableId);
            request.Append(" RFID=").Append(rfid);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }


        private DataActionResult ExchangeDurable(string sourceId, string targetId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("ExchangeDurable");
            request.Append(" SOURCE_ID=").Append(sourceId);
            request.Append(" TARGET_ID=").Append(targetId);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }


        private DataActionResult EditDurable(string sourceId, string targetId, IList<string> wfIds)
        {
            List<string> encoded = new List<string>();
            foreach (string id in wfIds)
            {
                encoded.Add(Uri.EscapeDataString(id));
            }

            StringBuilder request = new StringBuilder();
            request.Append("EditDurable");
            request.Append(" SOURCE_ID=").Append(sourceId);
            request.Append(" TARGET_ID=").Append(targetId);
            request.Append(" WF_IDS=").Append(string.Join(",", encoded));
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }


        private DataActionResult TransportDurable(string durableId, string location)
        {
            StringBuilder request = new StringBuilder();
            request.Append("TransportDurable");
            request.Append(" DURABLE_ID=").Append(durableId);
            request.Append(" LOCATION=").Append(location);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            this.cboType.DisplayMember = "TYPE_NM";
            this.cboType.ValueMember = "TYPE_ID";

            this.gridDurables.RowKeyMember = ServerFields.Durable.DurableId;

            this.gridWaferList.RowKeyMember = ServerFields.Unit.SlotNo;
            this.gridChipList.RowKeyMember = ServerFields.Unit.SlotNo;
            this.gridLamellaList.RowKeyMember = ServerFields.Unit.SlotNo;
            this.ApplyTypeLayout(DurableManagementPresenter.TypeFoup);

            this.fieldInfo.Columns = 5;

            this.gridHistory.RowKeyMember = "DURABLE_ID,TIMEKEY";

            this.PrepareUnitHistory(this.gridStubHistory);
            this.PrepareUnitHistory(this.gridLccHistory);

            this.PopulateMenu(this.menuDurable, "Execute", DurableManagementPresenter.MenuActions, this.OnDurableMenuItemClick);
            this.RefreshActionStates();

            this.LoadTypes();
        }

        private void ApplyTypeLayout(string durableType)
        {
            if (this.layoutType == durableType)
            {
                return;
            }

            this.layoutType = durableType;
            bool tray = durableType == DurableManagementPresenter.TypeTray;

            this.tabContent.SuspendLayout();
            this.tabHistory.SuspendLayout();

            try
            {
                this.tabContent.Controls.Remove(this.pageWaferList);
                this.tabContent.Controls.Remove(this.pageChipList);
                this.tabContent.Controls.Remove(this.pageLamellaList);
                this.tabHistory.Controls.Remove(this.pageStubHistory);
                this.tabHistory.Controls.Remove(this.pageLccHistory);

                if (tray)
                {
                    this.tabContent.Controls.Add(this.pageChipList);
                    this.tabContent.Controls.Add(this.pageLamellaList);
                    this.tabHistory.Controls.Add(this.pageStubHistory);
                    this.tabHistory.Controls.Add(this.pageLccHistory);
                }
                else
                {
                    this.tabContent.Controls.Add(this.pageWaferList);
                }
            }
            finally
            {
                this.tabContent.ResumeLayout(true);
                this.tabHistory.ResumeLayout(true);
            }

            this.tabContent.SelectedIndex = 0;
            this.tabHistory.SelectedIndex = 0;
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
                this.typeData = outcome.Value;
                this.cboType.DataSource = outcome.Value;
                this.cboType.SelectedValue = DurableManagementPresenter.TypeFoup;
            }
            finally
            {
                this.typeSetup = false;
            }

            this.ExecuteSearch();
        }

        private void OnTypeChanged(object sender, EventArgs e)
        {
            if (this.typeSetup)
            {
                return;
            }

            this.ExecuteSearch();
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            this.ExecuteSearch();
        }

        private string SelectedType()
        {
            string type = (Convert.ToString(this.cboType.SelectedValue) ?? string.Empty).Trim();

            return type.Length == 0 ? DurableManagementPresenter.TypeFoup : type;
        }

        private string SelectedDurableId()
        {
            DataRowView durable = this.gridDurables.SelectedItem as DataRowView;

            return durable == null ? string.Empty : DurableManagementPresenter.Id(durable.Row);
        }

        private async void ExecuteSearch(bool silent = false, string focusDurableId = null)
        {
            this.durableSearchVersion++;
            this.InvalidateChannel(channelEditContents);
            string durableType = this.SelectedType();
            this.newItems.BeginQuery();
            this.RefreshActionStates();
            string previousDurableId = this.SelectedDurableId();
            string keepDurableId = string.IsNullOrEmpty(focusDurableId) ? previousDurableId : focusDurableId;

            IDisposable busy = silent ? null : this.Busy("Loading durables...", durableType);

            LoadOutcome<DataTable> outcome;

            try
            {
                outcome = await this.FetchAsync(
                        channelDurables, () => this.RequestDurables(durableType), silent);
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

            this.newItemCriteria = NewItemTracker.Criteria(durableType);
            this.ApplyTypeLayout(durableType);
            this.BindDurables(
                    outcome.Value, keepDurableId,
                    string.IsNullOrEmpty(focusDurableId) ? string.Empty : previousDurableId, silent);

            if (!string.IsNullOrEmpty(focusDurableId) && this.SelectedDurableId() != focusDurableId)
            {
                this.ShowToast("Created durable " + focusDurableId + " is not visible in the current list. Check the filters or refresh.", ToastKind.Warning);
            }
        }

        private void PrepareUnitHistory(Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid)
        {
            grid.RowKeyMember = "WF_ID,TIMEKEY";
            grid.RowForegroundMember = DurableManagementPresenter.JudgeColorColumn;
            grid.CellLinkClick += this.OnReqSerialNoLinkClick;
        }

        private void BindDurables(DataTable durables, string keepDurableId, string fallbackDurableId, bool silent)
        {
            this.durableData = this.BindJudged(
                    DurableContracts.DurableTable, ref this.durableCurrent, ref this.durableReserved, durables);

            bool judged = Judged(this.durableCurrent);
            this.durableData = NewItemTint.Apply(
                    this.newItems.Accept(this.durableData, ServerFields.Durable.DurableId, this.newItemCriteria, judged), judged);
            this.gridDurables.RowColorMember = NewItemTint.Member(this.durableData);
            DataTable bound;

            if (!judged)
            {
                bound = this.durableData == null ? null : this.durableData.Copy();
                GridColumns.Of(bound).Apply(this.gridDurables);
            }
            else
            {
                bound = this.durableData;

                bool hasInvalid = TableJudgment.HasInvalidRows(this.durableData);

                GridColumns columns = GridColumns.Of(this.durableData)
                        .Badge(ServerFields.Durable.DurableStatCd, ServerFields.Durable.WfLoadStatCd.Column)
                        .Hide(NewItemTracker.ColumnName, NewItemTint.ColumnName);

                if (hasInvalid)
                {
                    IssueBadge(columns, true);
                }

                columns.Apply(this.gridDurables);
            }

            FieldDefinitions.Of(judged ? this.durableCurrent.Table : this.durableData)
                    .Hide(
                            ServerFields.Carrier.UseFoupCount, ServerFields.Carrier.FoupCapa,
                            ServerFields.Carrier.UseStubCount, ServerFields.Carrier.StubCapa,
                            ServerFields.Carrier.UseLccCount, ServerFields.Carrier.LccCapa)
                    .Span("DESCRIPTION", 5)
                    .Apply(this.fieldInfo);

            string selectId = DurableManagementPresenter.SelectionAfterBind(this.durableData, keepDurableId, fallbackDurableId);

            this.gridBinding = true;

            try
            {
                this.boundDurables = bound;
                this.gridDurables.DataSource = bound;

                if (selectId.Length > 0)
                {
                    this.SelectDurable(selectId);
                }
                else
                {
                    this.gridDurables.SelectedIndex = -1;
                }
            }
            finally
            {
                this.gridBinding = false;
            }

            this.durableCard.TitleRightText = this.durableData == null
                    ? string.Empty
                    : this.durableData.Rows.Count.ToString("N0") + " durables";

            this.RefreshContractNotice();
            this.ApplyDurableSelection(silent);
        }

        private DataTable BindJudged(string tableId, ref TableResponse current, ref TableResponse reserved, DataTable incoming)
        {
            reserved = null;

            if (incoming == null)
            {
                current = null;
                return null;
            }

            TableReception reception = TableJudgment.Receive(
                    tableId, incoming, Contracts, DurableManagementPresenter.ScreenColumns);

            if (reception.IsMissingRequired)
            {
                current = reception.Response;
                return reception.DisplayCopy;
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

        private static bool Judged(TableResponse current)
        {
            return current != null && current.State != TableResponseState.MissingRequired;
        }

        private static void IssueBadge(GridColumns columns, bool show)
        {
            if (!show)
            {
                columns.Hide(TableJudgment.IssueColumn);
                return;
            }

            columns.First(TableJudgment.IssueColumn);

            foreach (ModernDataGridColumn column in columns.ToArray())
            {
                if (column.DataPropertyName == TableJudgment.IssueColumn)
                {
                    column.Kind = GridColumnKind.Badge;
                    column.BadgeAccentValues = TableJudgment.IssueInvalid;
                    column.TextAlignment = GridTextAlignment.Center;
                }
            }
        }

        private void RefreshContractNotice()
        {
            string text = DurableManagementPresenter.BannerText(
                    new TableResponse[] { this.durableCurrent },
                    new TableResponse[] { this.durableReserved },
                    Contracts);

            this.SetContractNotice(text);
        }

        private void SelectDurable(string durableId)
        {
            DataTable table = this.boundDurables ?? this.durableData;

            if (table == null || durableId.Length == 0)
            {
                return;
            }

            DataView view = table.DefaultView;

            for (int index = 0; index < view.Count; index++)
            {
                DataRow row = view[index].Row;

                if (DurableManagementPresenter.Id(row) == durableId && !TableJudgment.IsInvalid(row))
                {
                    this.gridDurables.SelectedIndex = index;
                    return;
                }
            }

            this.gridDurables.SelectedIndex = -1;
        }

        private void OnDurableSelectionChanged(object sender, EventArgs e)
        {
            if (this.gridBinding)
            {
                return;
            }

            this.ApplyDurableSelection(false);
        }

        private void ApplyDurableSelection(bool silent)
        {
            DataRowView durable = this.gridDurables.SelectedItem as DataRowView;

            if (durable == null)
            {
                this.fieldInfo.ClearValues();
                this.ClearDependents();
                this.RefreshActionStates();
                return;
            }

            this.fieldInfo.SetRow(durable.Row);

            this.ClearDependents();
            this.RefreshActionStates();

            if (!Judged(this.durableCurrent) || TableJudgment.IsInvalid(durable.Row))
            {
                return;
            }

            string durableId = DurableManagementPresenter.Id(durable.Row);

            this.pageHistory.Text = durableHistoryTitle + " — " + durableId;
            this.pageStubHistory.Text = stubHistoryTitle + " — " + durableId;
            this.pageLccHistory.Text = lccHistoryTitle + " — " + durableId;
            this.LoadDependents(durableId, silent);
        }

        private void ClearDependents()
        {
            this.InvalidateChannel(channelEditContents);
            this.InvalidateChannel(channelSlots);
            this.InvalidateChannel(channelWaferList);
            this.InvalidateChannel(channelChipList);
            this.InvalidateChannel(channelLamellaList);
            this.InvalidateChannel(channelHistory);
            this.InvalidateChannel(channelStubHistory);
            this.InvalidateChannel(channelLccHistory);

            this.slotData = null;
            this.gridWaferList.DataSource = null;
            this.gridChipList.DataSource = null;
            this.gridLamellaList.DataSource = null;
            this.pageWaferList.BadgeCount = 0;
            this.pageChipList.BadgeCount = 0;
            this.pageLamellaList.BadgeCount = 0;
            this.historyData = null;
            this.gridHistory.DataSource = null;
            this.stubHistoryData = null;
            this.gridStubHistory.DataSource = null;
            this.lccHistoryData = null;
            this.gridLccHistory.DataSource = null;
            this.pageHistory.Text = durableHistoryTitle;
            this.pageStubHistory.Text = stubHistoryTitle;
            this.pageLccHistory.Text = lccHistoryTitle;
        }

        private void LoadDependents(string durableId, bool silent)
        {
            this.LoadDependent(channelSlots, () => this.RequestSlots(durableId, string.Empty), this.BindSlots, silent);
            this.LoadDependent(channelHistory, () => this.RequestHistory(durableId), this.BindHistory, silent);

            if (this.SelectedType() != DurableManagementPresenter.TypeTray)
            {
                this.LoadDependent(
                        channelWaferList,
                        () => this.RequestSlots(durableId, DurableManagementPresenter.SubTypeWafer),
                        table => this.BindContent(this.gridWaferList, this.pageWaferList, table, "Wafer Id"), silent);
                return;
            }

            this.LoadDependent(
                    channelChipList,
                    () => this.RequestSlots(durableId, DurableManagementPresenter.SubTypeChip),
                    table => this.BindContent(this.gridChipList, this.pageChipList, table, "Chip Id"), silent);
            this.LoadDependent(
                    channelLamellaList,
                    () => this.RequestSlots(durableId, DurableManagementPresenter.SubTypeLamella),
                    table => this.BindContent(this.gridLamellaList, this.pageLamellaList, table, "Lamella Id"), silent);
            this.LoadDependent(
                    channelStubHistory,
                    () => this.RequestUnitHistory(durableId, DurableManagementPresenter.SubTypeChip),
                    this.BindStubHistory, silent);
            this.LoadDependent(
                    channelLccHistory,
                    () => this.RequestUnitHistory(durableId, DurableManagementPresenter.SubTypeLamella),
                    this.BindLccHistory, silent);
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

        private void BindSlots(DataTable slots)
        {
            this.slotData = slots;
        }

        private void BindContent(
                Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid,
                Modern.Lab.WinForms.Controls.Layout.ModernTabPage page,
                DataTable table, string idCaption)
        {
            GridColumns.Of(table)
                    .Caption(ServerFields.Unit.WfId, idCaption)
                    .Bind(grid);

            page.BadgeCount = table == null ? 0 : table.Rows.Count;
        }

        private void BindHistory(DataTable history)
        {
            this.historyData = history;

            GridColumns.Of(history)
                    .Badge(ServerFields.Durable.DurableStatCd, ServerFields.Durable.WfLoadStatCd.Column)
                    .Bind(this.gridHistory);
        }

        private void BindStubHistory(DataTable history)
        {
            this.stubHistoryData = DurableManagementPresenter.MarkJudgeColors(history);
            this.BindUnitHistory(this.gridStubHistory, this.stubHistoryData, "Chip Id");
        }

        private void BindLccHistory(DataTable history)
        {
            this.lccHistoryData = DurableManagementPresenter.MarkJudgeColors(history);
            this.BindUnitHistory(this.gridLccHistory, this.lccHistoryData, "Lamella Id");
        }

        private void BindUnitHistory(
                Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid, DataTable history, string idCaption)
        {
            GridColumns.Of(history)
                    .Caption(ServerFields.Unit.WfId, idCaption)
                    .Link(ServerFields.Lot.ReqSerialNo)
                    .Bind(grid);
        }

        private void RefreshActionStates()
        {
            DataRowView durable = this.gridDurables.SelectedItem as DataRowView;
            DataRow row = durable == null ? null : durable.Row;
            ActionGate gate = this.BuildGate(row);

            this.btnCreate.Enabled = DurableManagementPresenter.CanExecute(DurableManagementPresenter.ActionCreate, row);
            this.btnRfId.Enabled = this.ActionEnabled(DurableManagementPresenter.ActionRfId, row, gate);
            this.btnExchange.Enabled = this.ActionEnabled(DurableManagementPresenter.ActionExchange, row, gate);
            this.btnEdit.Enabled = this.ActionEnabled(DurableManagementPresenter.ActionEdit, row, gate);
            this.btnTransport.Enabled = this.ActionEnabled(DurableManagementPresenter.ActionTransport, row, gate);

            this.lblTarget.Text = DurableManagementPresenter.StatusLineText(row, gate);
        }

        private bool ActionEnabled(string key, DataRow row, ActionGate gate)
        {
            return gate.CanExecute(key) && DurableManagementPresenter.CanExecute(key, row);
        }


        private ActionGate BuildGate(DataRow durable)
        {
            ActionGate gate = new ActionGate(Contracts);
            if (!this.newItems.ActionsReady)
            {
                return gate;
            }
            Observe(gate, DurableContracts.DurableTable, this.durableCurrent, this.durableData, durable);
            return gate;
        }

        private static void Observe(ActionGate gate, string tableId, TableResponse current, DataTable bound, DataRow selected)
        {
            if (current == null || bound == null)
            {
                return;
            }

            gate.Observe(tableId, current, RowIn(current, bound, selected));
        }

        private static DataRow RowIn(TableResponse response, DataTable bound, DataRow selected)
        {
            if (selected == null || response.Table == null)
            {
                return null;
            }

            int index = bound.Rows.IndexOf(selected);

            if (index < 0 || index >= response.Table.Rows.Count)
            {
                return null;
            }

            return response.Table.Rows[index];
        }

        private void OnCreateClick(object sender, EventArgs e)
        {
            this.ExecuteAction(DurableManagementPresenter.ActionCreate);
        }

        private void OnRfIdClick(object sender, EventArgs e)
        {
            this.ExecuteAction(DurableManagementPresenter.ActionRfId);
        }

        private void OnExchangeClick(object sender, EventArgs e)
        {
            this.ExecuteAction(DurableManagementPresenter.ActionExchange);
        }

        private void OnEditClick(object sender, EventArgs e)
        {
            this.ExecuteAction(DurableManagementPresenter.ActionEdit);
        }

        private void OnTransportClick(object sender, EventArgs e)
        {
            this.ExecuteAction(DurableManagementPresenter.ActionTransport);
        }

        private void ExecuteAction(string key)
        {
            DataRowView durable = this.gridDurables.SelectedItem as DataRowView;
            DataRow row = durable == null ? null : durable.Row;

            bool allowed = key == DurableManagementPresenter.ActionCreate
                    ? DurableManagementPresenter.CanExecute(key, row)
                    : this.ActionEnabled(key, row, this.BuildGate(row));

            if (!allowed)
            {
                return;
            }

            switch (key)
            {
                case DurableManagementPresenter.ActionCreate:
                    this.ShowCreateDialog();
                    break;

                case DurableManagementPresenter.ActionRfId:
                    this.ShowRfIdDialog(row);
                    break;

                case DurableManagementPresenter.ActionExchange:
                    this.ShowExchangeDialog(row);
                    break;

                case DurableManagementPresenter.ActionEdit:
                    this.OpenCarrierEditor(row);
                    break;

                case DurableManagementPresenter.ActionTransport:
                    this.ShowTransportDialog(row);
                    break;
            }
        }

        private async void ShowCreateDialog()
        {
            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelCategories, () => this.RequestCategories());

            if (!outcome.IsCurrent || outcome.Failure != null)
            {
                return;
            }

            this.candidateTypes = CandidateGate.Receive(CandidateContracts, DurableCandidateContracts.TypeTable, this.typeData);
            this.candidateCategories = CandidateGate.Receive(CandidateContracts, DurableCandidateContracts.CategoryTable, outcome.Value);

            DurableCreateDialogOptions options = new DurableCreateDialogOptions();
            options.Title = "Create Durable";
            options.OkText = "Create";

            options.Types = CandidateGate.Candidates(this.candidateTypes);

            options.SelectedType = this.SelectedType();

            options.Categories = CandidateGate.Candidates(this.candidateCategories);
            options.CandidateIssue = CandidateGate.ShapeIssue(this.candidateCategories, "Category list");

            if (options.CandidateIssue.Length == 0)
            {
                options.CandidateIssue = CandidateGate.ShapeIssue(this.candidateTypes, "Type list");
            }

            this.OpenCreateDialog(options);
        }

        protected virtual void OpenCreateDialog(DurableCreateDialogOptions options)
        {
            using (DurableCreateDialogForm dialog = new DurableCreateDialogForm(options))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.ProcessCreate(dialog.DurableType, dialog.Category, dialog.Description);
                }
            }
        }

        protected virtual void ShowRfIdDialog(DataRow durable)
        {
            DurableRfIdDialogOptions options = new DurableRfIdDialogOptions();
            options.Title = "Assign RFID";
            options.OkText = "Assign";
            options.Source = durable;

            using (DurableRfIdDialogForm dialog = new DurableRfIdDialogForm(options))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.ProcessRfId(DurableManagementPresenter.Id(durable), dialog.RfId);
                }
            }
        }

        private DataTable TargetCandidates()
        {
            this.candidateTargets = CandidateGate.Receive(CandidateContracts, DurableCandidateContracts.TargetTable, this.durableData);
            return CandidateGate.Candidates(this.candidateTargets);
        }

        protected virtual async void ShowExchangeDialog(DataRow durable)
        {
            string sourceId = DurableManagementPresenter.Id(durable);
            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelEditContents, () => this.RequestSlots(sourceId, string.Empty));
            if (!outcome.IsCurrent || outcome.Failure != null)
            {
                return;
            }
            DataRowView selected = this.gridDurables.SelectedItem as DataRowView;
            DataRow current = selected == null ? null : selected.Row;
            if (DurableManagementPresenter.Id(current) != sourceId
                    || !this.ActionEnabled(DurableManagementPresenter.ActionExchange, current, this.BuildGate(current)))
            {
                return;
            }
            DurableExchangeDialogOptions options = new DurableExchangeDialogOptions();
            options.Title = "Exchange — move all contents into an empty " + DurableManagementPresenter.Type(durable);
            options.OkText = "Exchange";
            options.Source = current;
            options.Targets = DurableManagementPresenter.ExchangeTargets(this.TargetCandidates(), current);
            options.TargetIssue = CandidateGate.ShapeIssue(this.candidateTargets, "Durable list");
            options.Slots = outcome.Value;

            this.OpenExchangeDialog(options);
        }

        protected virtual void OpenExchangeDialog(DurableExchangeDialogOptions options)
        {
            using (DurableExchangeDialogForm dialog = new DurableExchangeDialogForm(options))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.ProcessExchange(DurableManagementPresenter.Id(options.Source), dialog.TargetDurableId);
                }
            }
        }

        protected virtual async void ShowEditDialog(DataRow durable)
        {
            string sourceId = DurableManagementPresenter.Id(durable);
            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelEditContents, () => this.RequestSlots(sourceId, string.Empty));
            if (!outcome.IsCurrent || outcome.Failure != null)
            {
                return;
            }

            DataRowView selected = this.gridDurables.SelectedItem as DataRowView;
            DataRow current = selected == null ? null : selected.Row;
            if (DurableManagementPresenter.Id(current) != sourceId
                    || !this.ActionEnabled(DurableManagementPresenter.ActionEdit, current, this.BuildGate(current)))
            {
                return;
            }

            DurableEditDialogOptions options = new DurableEditDialogOptions();
            options.Title = "Edit — move contents (all or checked items)";
            options.OkText = "Move";
            options.Source = current;
            options.Targets = DurableManagementPresenter.EditTargets(this.TargetCandidates(), current);
            options.TargetIssue = CandidateGate.ShapeIssue(this.candidateTargets, "Durable list");

            options.Slots = outcome.Value;
            options.LoadTargetContents = targetId => this.RequestSlots(targetId, string.Empty);

            this.OpenEditDialog(options);
        }

        protected virtual void OpenEditDialog(DurableEditDialogOptions options)
        {
            using (DurableEditDialogForm dialog = new DurableEditDialogForm(options))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.ProcessEdit(
                            DurableManagementPresenter.Id(options.Source), dialog.TargetDurableId, dialog.SelectedContentIds);
                }
            }
        }

        protected virtual void OpenCarrierEditor(DataRow durable)
        {
            using (CarrierEditForm editor = new CarrierEditForm(
                    DurableManagementPresenter.Type(durable),
                    DurableManagementPresenter.Id(durable)))
            {
                editor.ShowDialog(this);

                if (editor.SourceUnavailable || editor.DurableChanged)
                {
                    this.ExecuteSearch(true);
                }
            }
        }

        protected virtual async void ShowTransportDialog(DataRow durable)
        {
            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelLocations, () => this.RequestLocations());

            if (!outcome.IsCurrent || outcome.Failure != null || !this.newItems.ActionsReady)
            {
                return;
            }

            this.candidateLocations = CandidateGate.Receive(CandidateContracts, DurableCandidateContracts.LocationTable, outcome.Value);

            DurableTransportDialogOptions options = new DurableTransportDialogOptions();
            options.Title = "TransPort — select destination";
            options.OkText = "TransPort";
            options.Source = durable;
            options.Targets = DurableManagementPresenter.TransportTargets(
                    CandidateGate.Candidates(this.candidateLocations), durable, this.TransportLocationId(durable));
            options.TargetIssue = CandidateGate.ShapeIssue(this.candidateLocations, "Destination list");

            options.TargetDisplayMember = DurableManagementPresenter.TransportTargetColumn;
            options.TargetValueMember = DurableManagementPresenter.TransportTargetColumn;

            this.OpenTransportDialog(options);
        }

        protected virtual void OpenTransportDialog(DurableTransportDialogOptions options)
        {
            using (DurableTransportDialogForm dialog = new DurableTransportDialogForm(options))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.ProcessTransport(DurableManagementPresenter.Id(options.Source), dialog.Destination);
                }
            }
        }


        private DataRow DurableRow(string durableId)
        {
            if (this.durableData == null || string.IsNullOrEmpty(durableId))
            {
                return null;
            }

            foreach (DataRow row in this.durableData.Rows)
            {
                if (DurableManagementPresenter.Id(row) == durableId.Trim())
                {
                    return row;
                }
            }

            return null;
        }

        private bool CandidateGateOpen(string key, string slotLabel, TableResponse response, Func<ActionGate, ActionGate> observe)
        {
            if (response == null)
            {
                this.ShowToast(DurableManagementPresenter.ActionLabel(key) + ": candidates not loaded — open the dialog again.", ToastKind.Warning);
                return false;
            }

            ActionGate gate = observe(new ActionGate(CandidateContracts));

            if (gate.CanExecute(key))
            {
                return true;
            }

            this.ShowToast(
                    DurableManagementPresenter.ActionLabel(key) + ": " + CandidateGate.ReasonText(gate, key, delegate(string slot) { return slotLabel; }),
                    ToastKind.Warning);
            return false;
        }

        private bool TargetGateOpen(string key, string sourceId, string targetId, bool emptyOnly)
        {
            if (this.durableData == null || this.candidateTargets == null)
            {
                this.ShowToast(DurableManagementPresenter.ActionLabel(key) + ": candidates not loaded — open the dialog again.", ToastKind.Warning);
                return false;
            }

            DataRow source = this.DurableRow(sourceId);

            if (source == null || !this.ActionEnabled(key, source, this.BuildGate(source)))
            {
                this.ShowToast(DurableManagementPresenter.ActionLabel(key) + ": " + sourceId + " is no longer eligible in the current list.", ToastKind.Warning);
                return false;
            }

            TableResponse current = CandidateGate.Receive(CandidateContracts, DurableCandidateContracts.TargetTable, this.durableData);
            DataRow target = CandidateGate.Find(current, ServerFields.Durable.DurableId, targetId);
            bool open = this.CandidateGateOpen(
                    key, "Target", current,
                    gate => gate.Observe(
                            DurableCandidateContracts.TargetTable, current,
                            SelectionSlot.Of(DurableCandidateContracts.TargetSlot, target)));

            if (!open)
            {
                return false;
            }

            DataTable candidates = CandidateGate.Candidates(current);
            DataTable allowed = emptyOnly
                    ? DurableManagementPresenter.ExchangeTargets(candidates, source)
                    : DurableManagementPresenter.EditTargets(candidates, source);

            if (!ContainsId(allowed, targetId))
            {
                this.ShowToast(DurableManagementPresenter.ActionLabel(key) + ": Target is not a valid candidate for " + sourceId + " in the current list.", ToastKind.Warning);
                return false;
            }

            return true;
        }

        private static bool ContainsId(DataTable table, string durableId)
        {
            string wanted = (durableId ?? string.Empty).Trim();

            foreach (DataRow row in table.Rows)
            {
                if (DurableManagementPresenter.Id(row) == wanted)
                {
                    return true;
                }
            }

            return false;
        }


        private void ProcessCreate(string durableType, string category, string description)
        {
            bool open = this.CandidateGateOpen(
                    DurableManagementPresenter.ActionCreate, string.Empty, this.candidateTypes == null ? null : this.candidateCategories,
                    gate => gate
                            .Observe(
                                    DurableCandidateContracts.TypeTable, this.candidateTypes,
                                    CandidateGate.Find(this.candidateTypes, DurableManagementPresenter.TypeIdColumn, durableType))
                            .Observe(
                                    DurableCandidateContracts.CategoryTable, this.candidateCategories,
                                    CandidateGate.Find(
                                            this.candidateCategories,
                                            new string[] { DurableManagementPresenter.CategoryTypeColumn, DurableManagementPresenter.CategoryColumn },
                                            new string[] { durableType, category })));

            if (!open)
            {
                return;
            }

            int searchVersion = this.durableSearchVersion;
            string selectedId = this.SelectedDurableId();
            this.Process(
                    () => this.CreateDurable(durableType, category, description),
                    "Creating " + category + " durable…",
                    reply => this.RefreshCreatedDurable(reply, durableType, searchVersion, selectedId));
        }

        private void RefreshCreatedDurable(DataActionResult reply, string durableType, int searchVersion, string selectedId)
        {
            string[] values = new string[0];
            try
            {
                if (!string.IsNullOrWhiteSpace(reply.Text(ServerMessageFormat.FieldResultMessage)))
                {
                    values = reply.Values;
                }
            }
            catch (Exception)
            {
                this.ExecuteSearch(true);
                return;
            }
            string createdId = values.Length == 1 ? (values[0] ?? string.Empty).Trim() : string.Empty;
            if (createdId.Length == 0 || this.durableSearchVersion != searchVersion || this.SelectedDurableId() != selectedId)
            {
                this.ExecuteSearch(true);
                return;
            }
            this.typeSetup = true;
            try { this.cboType.SelectedValue = durableType; }
            finally { this.typeSetup = false; }
            this.ExecuteSearch(true, createdId);
        }

        private void ProcessRfId(string durableId, string rfid)
        {
            this.Process(
                    () => this.SetRfId(durableId, rfid),
                    "Assigning RFID…",
                    () => this.ExecuteSearch(true));
        }

        private void ProcessExchange(string sourceId, string targetId)
        {
            if (!this.TargetGateOpen(DurableManagementPresenter.ActionExchange, sourceId, targetId, true))
            {
                return;
            }

            this.Process(
                    () => this.ExchangeDurable(sourceId, targetId),
                    "Exchanging " + sourceId + " → " + targetId + "…",
                    () => this.ExecuteSearch(true));
        }

        private void ProcessEdit(string sourceId, string targetId, IList<string> wfIds)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId)
                    || sourceId == targetId || !DurableDialogData.ValidContentIds(wfIds))
            {
                this.ShowToast("Select different durables and valid content Ids.", ToastKind.Warning);
                return;
            }

            if (!this.TargetGateOpen(DurableManagementPresenter.ActionEdit, sourceId, targetId, false))
            {
                return;
            }

            string capacityIssue = DurableDialogData.CapacityIssue(this.DurableRow(targetId), wfIds.Count);
            if (capacityIssue.Length > 0)
            {
                this.ShowToast(capacityIssue, ToastKind.Warning);
                return;
            }

            List<string> ids = new List<string>(wfIds);
            this.Process(
                    () => this.EditDurable(sourceId, targetId, ids),
                    "Moving " + sourceId + " → " + targetId + "…",
                    () => this.ExecuteSearch(true));
        }

        private void ProcessTransport(string durableId, string location)
        {
            DataRow source = this.DurableRow(durableId);

            if (source == null || !this.ActionEnabled(DurableManagementPresenter.ActionTransport, source, this.BuildGate(source)))
            {
                this.ShowToast("TransPort: " + durableId + " is no longer eligible in the current list.", ToastKind.Warning);
                return;
            }

            bool open = this.CandidateGateOpen(
                    DurableManagementPresenter.ActionTransport, string.Empty, this.candidateLocations,
                    gate => gate.Observe(
                            DurableCandidateContracts.LocationTable, this.candidateLocations,
                            CandidateGate.Find(this.candidateLocations, DurableManagementPresenter.TransportTargetColumn, location)));

            if (!open)
            {
                return;
            }

            DataTable allowed = DurableManagementPresenter.TransportTargets(
                    CandidateGate.Candidates(this.candidateLocations), source, this.TransportLocationId(source));

            if (source == null || CandidateGate.Find(
                    CandidateGate.Receive(CandidateContracts, DurableCandidateContracts.LocationTable, allowed),
                    DurableManagementPresenter.TransportTargetColumn, location) == null)
            {
                this.ShowToast("TransPort: Destination is not a valid candidate for " + durableId + ".", ToastKind.Warning);
                return;
            }

            this.Process(
                    () => this.TransportDurable(durableId, location),
                    "Transporting " + durableId + " → " + location + "…",
                    () => this.ExecuteSearch(true));
        }

        private void Process(Func<DataActionResult> call, string busyText, Action<DataActionResult> refresh)
        {
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

        private void Process(Func<DataActionResult> call, string busyText, Action refresh)
        {
            if (!this.newItems.ActionsReady)
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
                        refresh();
                    },
                    busyText);
        }

        private void PopulateMenu(
                ContextMenuStrip menu, string headerText,
                IList<DurableAction> actions, EventHandler onClick)
        {
            menu.Items.Clear();

            if (!string.IsNullOrEmpty(headerText))
            {
                menu.Items.Add(new ToolStripLabel(headerText));
            }

            foreach (DurableAction action in actions)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(action.Label);
                item.Tag = action.Key;
                item.Click += onClick;
                menu.Items.Add(item);
            }
        }

        private void OnMenuDurableOpening(object sender, CancelEventArgs e)
        {
            DataRowView durable = this.gridDurables.SelectedItem as DataRowView;

            if (durable == null)
            {
                e.Cancel = true;
                return;
            }

            ActionGate gate = this.BuildGate(durable.Row);

            foreach (object entry in this.menuDurable.Items)
            {
                ToolStripMenuItem item = entry as ToolStripMenuItem;
                string key = item == null ? null : item.Tag as string;

                if (key != null && DurableManagementPresenter.IsActionKey(key))
                {
                    item.Enabled = this.ActionEnabled(key, durable.Row, gate);
                    item.ToolTipText = DurableManagementPresenter.ActionReasonText(gate, key);
                }
            }
        }

        private void OnDurableMenuItemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item != null)
            {
                this.ExecuteAction(item.Tag as string);
            }
        }
    }
}
