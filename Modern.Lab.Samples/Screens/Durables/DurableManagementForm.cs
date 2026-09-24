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
using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.WinForms.Controls.Display;
using Modern.Lab.Hosting.NewItems;

namespace Modern.Lab.Samples
{

    public partial class DurableManagementForm : ModernFormBase
    {
        private DataTable durableData;
        private DataTable boundDurables;
        private readonly NewItemTracker newItems = new NewItemTracker();
        private int durableSearchVersion;
        private int durableSelectionVersion;
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
        private const string channelCategories = "categories";
        private const string channelEditContents = "editContents";
        private const string durableHistoryTitle = "Durable History";
        private const string stubHistoryTitle = "Stub History";
        private const string lccHistoryTitle = "Lcc History";

        private DataTable typeData;
        private bool typeSetup;
        private bool gridBinding;

        private string layoutType = string.Empty;

        private static readonly ResponseContractSet Contracts = DurableContracts.Build();

        private DataTable categoryData;

        private TableResponse durableCurrent;
        private TableResponse durableReserved;

        private const double issueWidth = 84d;

        private readonly DependentCover contentCover;
        private readonly DependentCover historyCover;

        public DurableManagementForm()
        {
            this.InitializeComponent();

            this.contentCover = new DependentCover(this.contentBusy);
            this.historyCover = new DependentCover(this.historyBusy);

            this.InitializeModernForm(this.midPanel);

            this.DeferredResize = true;

            this.RegisterFindShortcut(this.gridHistory, this.gridStubHistory, this.gridLccHistory);

            this.menuDurable.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
        }

        protected override void ShowRequestInfoDialog(DataTable requestInfo)
        {
            using (RequestInfoDialogForm dialog = new RequestInfoDialogForm())
            {
                dialog.SetRequest(requestInfo, RequestSpecimenFirstColumn, RequestPurposeColumn);
                dialog.ShowDialog(this);
            }
        }

        protected virtual string TransportLocationId(DataRow durable)
        {
            return DurableManagementPresenter.Location(durable);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            this.cboType.DisplayMember = ServerFields.Durable.TypVal;
            this.cboType.ValueMember = ServerFields.Durable.TypVal;

            this.gridDurables.RowKeyMember = ServerFields.Durable.DurableId;

            this.gridWaferList.RowKeyMember = ServerFields.Wafer.SlotNo;
            this.gridChipList.RowKeyMember = ServerFields.Wafer.SlotNo;
            this.gridLamellaList.RowKeyMember = ServerFields.Wafer.SlotNo;
            this.ApplyTypeLayout(ServerFields.Durable.Foup);

            this.fieldInfo.Columns = 5;

            this.gridHistory.RowKeyMember = ServerFields.Durable.DurableId + "," + ServerFields.Item.TimeKey;

            this.PrepareWaferHistory(this.gridStubHistory);
            this.PrepareWaferHistory(this.gridLccHistory);

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
            bool tray = durableType == ServerFields.Durable.Tray;

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
                this.cboType.SelectedValue = ServerFields.Durable.Foup;
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
            string durableTyp = (Convert.ToString(this.cboType.SelectedValue) ?? string.Empty).Trim();

            return durableTyp.Length == 0 ? ServerFields.Durable.Foup : durableTyp;
        }

        private string SelectedDurableId()
        {
            DataRowView durable = this.gridDurables.SelectedItem as DataRowView;

            return durable == null ? string.Empty : DurableManagementPresenter.Id(durable.Row);
        }

        private async void ExecuteSearch(bool silent = false, string focusDurableId = null, bool selectNewItems = true)
        {
            this.durableSearchVersion++;
            this.InvalidateChannel(channelEditContents);
            string durableType = this.SelectedType();
            this.newItems.BeginQuery();
            this.RefreshActionStates();
            string previousDurableId = this.SelectedDurableId();
            string keepDurableId = previousDurableId;
            int selectionVersion = this.durableSelectionVersion;

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
            if (this.durableSelectionVersion != selectionVersion)
            {
                keepDurableId = this.SelectedDurableId();
                focusDurableId = null;
                selectNewItems = false;
            }
            this.ApplyTypeLayout(durableType);
            this.BindDurables(
                    outcome.Value, keepDurableId,
                    focusDurableId, silent, selectNewItems);

            if (!string.IsNullOrEmpty(focusDurableId) && this.SelectedDurableId() != focusDurableId)
            {
                this.ShowWarningMessage("Created durable " + focusDurableId + " is not visible in the current list. Check the filters or refresh.");
            }
        }

        private void PrepareWaferHistory(Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid)
        {
            grid.RowKeyMember = ServerFields.Wafer.WfId + "," + ServerFields.Item.TimeKey;
            grid.RowForegroundMember = JudgeResult.ColorColumn;
            grid.CellLinkClick += this.OnReqSerialNoLinkClick;
        }

        private void BindDurables(DataTable durables, string keepDurableId, string createdId, bool silent, bool selectNewItems = true)
        {
            this.durableData = this.BindJudged(
                    DurableContracts.DurableTable, ref this.durableCurrent, ref this.durableReserved, durables, silent);

            bool judged = Judged(this.durableCurrent);
            this.durableData = this.newItems.Accept(this.durableData, ServerFields.Durable.DurableId, this.newItemCriteria, judged);
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

                GridColumns columns = DurableColumns(this.durableData);

                if (hasInvalid)
                {
                    TableJudgment.IssueBadge(columns, true);
                }

                columns.Apply(this.gridDurables);
            }

            DurableFields(judged ? this.durableCurrent.Table : this.durableData).Apply(this.fieldInfo);

            string selectId = DurableManagementPresenter.SelectionAfterBind(this.durableData, keepDurableId);

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
                NewItemTint.Bind(this.gridDurables, this.newItems, ServerFields.Durable.DurableId, createdId, selectNewItems);
            }
            finally
            {
                this.gridBinding = false;
            }

            this.durableCard.TitleRightText = this.durableData == null
                    ? string.Empty
                    : this.durableData.Rows.Count.ToString("N0") + " durables";

            this.ApplyDurableSelection(silent);
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
                    tableId, incoming, Contracts, DurableManagementPresenter.ScreenColumns);

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

        private static bool Judged(TableResponse current)
        {
            return current != null;
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

            this.durableSelectionVersion++;
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

            bool loadable = Judged(this.durableCurrent) && !TableJudgment.IsInvalid(durable.Row);

            if (loadable)
            {
                this.InvalidateDependents();
            }
            else
            {
                this.ClearDependents();
            }

            this.RefreshActionStates();

            if (!loadable)
            {
                return;
            }

            string durableId = DurableManagementPresenter.Id(durable.Row);

            this.pageHistory.Text = durableHistoryTitle + " — " + durableId;
            this.pageStubHistory.Text = stubHistoryTitle + " — " + durableId;
            this.pageLccHistory.Text = lccHistoryTitle + " — " + durableId;
            this.LoadDependents(durableId, silent);
        }

        private void InvalidateDependents()
        {
            this.InvalidateChannel(channelEditContents);
            this.InvalidateChannel(channelSlots);
            this.InvalidateChannel(channelWaferList);
            this.InvalidateChannel(channelChipList);
            this.InvalidateChannel(channelLamellaList);
            this.InvalidateChannel(channelHistory);
            this.InvalidateChannel(channelStubHistory);
            this.InvalidateChannel(channelLccHistory);
        }

        private void ClearDependents()
        {
            this.contentCover.Reset();
            this.historyCover.Reset();
            this.InvalidateDependents();

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
            this.LoadDependent(channelSlots, () => this.RequestSlots(durableId, string.Empty), this.BindSlots, silent, this.contentCover);
            this.LoadDependent(channelHistory, () => this.RequestHistory(durableId), this.BindHistory, silent, this.historyCover);

            if (this.SelectedType() != ServerFields.Durable.Tray)
            {
                this.LoadDependent(
                        channelWaferList,
                        () => this.RequestSlots(durableId, ServerFields.Lot.SubProdTyp.Wafer),
                        table => this.BindContent(this.gridWaferList, this.pageWaferList, table, "Wafer Id"), silent, this.contentCover);
                return;
            }

            this.LoadDependent(
                    channelChipList,
                    () => this.RequestSlots(durableId, ServerFields.Lot.SubProdTyp.Chip),
                    table => this.BindContent(this.gridChipList, this.pageChipList, table, "Chip Id"), silent, this.contentCover);
            this.LoadDependent(
                    channelLamellaList,
                    () => this.RequestSlots(durableId, ServerFields.Lot.SubProdTyp.Lamella),
                    table => this.BindContent(this.gridLamellaList, this.pageLamellaList, table, "Lamella Id"), silent, this.contentCover);
            this.LoadDependent(
                    channelStubHistory,
                    () => this.RequestWaferHistory(durableId, ServerFields.Lot.SubProdTyp.Chip),
                    this.BindStubHistory, silent, this.historyCover);
            this.LoadDependent(
                    channelLccHistory,
                    () => this.RequestWaferHistory(durableId, ServerFields.Lot.SubProdTyp.Lamella),
                    this.BindLccHistory, silent, this.historyCover);
        }

        private async void LoadDependent(
                string channel, Func<DataTable> request, Action<DataTable> bind, bool silent, DependentCover cover)
        {
            cover.Begin();
            LoadOutcome<DataTable> outcome;

            try
            {
                outcome = await this.FetchAsync(channel, request, silent);
            }
            finally
            {
                cover.End();
            }

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
            SlotColumns(table, idCaption).Bind(grid);

            page.BadgeCount = table == null ? 0 : table.Rows.Count;
        }

        private void BindHistory(DataTable history)
        {
            this.historyData = history;

            DurableHistoryColumns(history).Bind(this.gridHistory);
        }

        private void BindStubHistory(DataTable history)
        {
            this.stubHistoryData = DurableManagementPresenter.MarkJudgeColors(history);
            this.BindWaferHistory(this.gridStubHistory, this.stubHistoryData, "Chip Id");
        }

        private void BindLccHistory(DataTable history)
        {
            this.lccHistoryData = DurableManagementPresenter.MarkJudgeColors(history);
            this.BindWaferHistory(this.gridLccHistory, this.lccHistoryData, "Lamella Id");
        }

        private void BindWaferHistory(
                Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid, DataTable history, string idCaption)
        {
            WaferHistoryColumns(history, idCaption).Bind(grid);
        }

        private void RefreshActionStates()
        {
            DataRowView durable = this.gridDurables.SelectedItem as DataRowView;
            DataRow row = durable == null ? null : durable.Row;

            this.ApplyActionReason(this.btnCreate, this.ActionReason(DurableManagementPresenter.ActionCreate, row));
            this.ApplyActionReason(this.btnRfId, this.ActionReason(DurableManagementPresenter.ActionRfId, row));
            this.ApplyActionReason(this.btnExchange, this.ActionReason(DurableManagementPresenter.ActionExchange, row));
            this.ApplyActionReason(this.btnEdit, this.ActionReason(DurableManagementPresenter.ActionEdit, row));
            this.ApplyActionReason(this.btnTransport, this.ActionReason(DurableManagementPresenter.ActionTransport, row));

            this.lblTarget.Text = DurableManagementPresenter.StatusLineText(row);
        }

        private string ActionReason(string key, DataRow row)
        {
            if (!this.newItems.ActionsReady)
            {
                return DurableManagementPresenter.LoadingReason;
            }

            return DurableActionReason(key, row);
        }

        private bool ActionEnabled(string key, DataRow row)
        {
            return this.ActionReason(key, row) == null;
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

            bool allowed = this.ActionEnabled(key, row);

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
                    this.OpenCarrierEditor(row, CarrierEditEntryMode.DurableExchange);
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

            string missing = ResponseColumns.Missing(
                    this.typeData,
                    ServerFields.Durable.TypVal,
                    ServerFields.Durable.TypVal);

            if (missing.Length == 0)
            {
                missing = ResponseColumns.Missing(
                        outcome.Value,
                        ServerFields.Durable.SubCatgCd,
                        ServerFields.Durable.DurableTyp);
            }

            if (missing.Length > 0)
            {
                this.ShowListUnavailable("Create Durable", "CreateDurable candidates", missing);
                return;
            }

            this.categoryData = outcome.Value;

            DurableCreateDialogOptions options = new DurableCreateDialogOptions();
            options.Title = "Create Durable";
            options.OkText = "Create";
            options.Types = this.typeData;
            options.SelectedType = this.SelectedType();
            options.Categories = this.categoryData;

            this.OpenCreateDialog(options);
        }

        protected virtual void OpenCreateDialog(DurableCreateDialogOptions options)
        {
            using (DurableCreateDialogForm dialog = new DurableCreateDialogForm(options))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.ProcessCreate(dialog.DurableTyp, dialog.Category, dialog.Description);
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
            return this.durableData;
        }

        protected virtual void OpenCarrierEditor(DataRow durable)
        {
            this.OpenCarrierEditor(durable, CarrierEditEntryMode.DurableEdit);
        }

        protected virtual void OpenCarrierEditor(DataRow durable, CarrierEditEntryMode entryMode)
        {
            using (CarrierEditForm editor = new CarrierEditForm(
                    DurableManagementPresenter.Type(durable),
                    DurableManagementPresenter.Id(durable),
                    entryMode))
            {
                editor.ShowDialog(this);

                if (editor.SourceUnavailable || editor.DurableChanged)
                {
                    this.ExecuteSearch(true);
                }
            }
        }

        protected virtual void ShowTransportDialog(DataRow durable)
        {
            DurableTransportDialogOptions options = new DurableTransportDialogOptions();
            options.Title = "TransPort — select destination";
            options.OkText = "TransPort";
            options.Source = durable;
            options.CurrentLocNm = DurableManagementPresenter.Location(durable);
            options.CurrentLocationId = this.TransportLocationId(durable);

            this.OpenTransportDialog(options);
        }

        protected virtual void OpenTransportDialog(DurableTransportDialogOptions options)
        {
            using (DurableTransportDialogForm dialog = new DurableTransportDialogForm(options))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.ProcessTransport(DurableManagementPresenter.Id(options.Source), dialog.LocNm, dialog.Destination);
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

        private void ShowListUnavailable(string title, string subject, string missing)
        {
            this.ShowErrorMessage(
                    title,
                    "The list needed for this action could not be used. Try again, or contact support.",
                    subject + " — missing column " + missing);
        }

        private void ProcessCreate(string durableType, string category, string description)
        {
            string reason = CreateReason();
            if (reason != null)
            {
                this.ShowWarningMessage(reason);
                return;
            }

            if (ResponseColumns.IsBlank(durableType) || ResponseColumns.IsBlank(category))
            {
                return;
            }

            int searchVersion = this.durableSearchVersion;
            int selectionVersion = this.durableSelectionVersion;
            string selectedId = this.SelectedDurableId();
            this.Process(
                    () => this.CreateDurable(durableType, category, description),
                    "Creating " + category + " durable…",
                    reply => this.RefreshCreatedDurable(reply, durableType, searchVersion, selectedId, selectionVersion));
        }

        private void RefreshCreatedDurable(DataActionResult reply, string durableType, int searchVersion, string selectedId, int selectionVersion)
        {
            if (this.durableSearchVersion != searchVersion || this.durableSelectionVersion != selectionVersion
                    || this.SelectedDurableId() != selectedId)
            {
                this.ExecuteSearch(true, null, false);
                return;
            }
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
            if (createdId.Length == 0)
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
            string reason = this.ActionReason(DurableManagementPresenter.ActionRfId, this.DurableRow(durableId));
            if (reason != null)
            {
                this.ShowWarningMessage(reason);
                return;
            }

            this.Process(
                    () => this.SetRfId(durableId, rfid),
                    "Assigning RFID…",
                    () => this.ExecuteSearch(true));
        }

        private void ProcessTransport(string durableId, string locNm, string location)
        {
            DataRow source = this.DurableRow(durableId);

            if (source == null || !this.ActionEnabled(DurableManagementPresenter.ActionTransport, source))
            {
                this.ShowWarningMessage("TransPort: " + durableId + " is no longer eligible in the current list.");
                return;
            }

            if (ResponseColumns.IsBlank(location) || ResponseColumns.IsBlank(locNm))
            {
                return;
            }

            this.Process(
                    () => this.TransportDurable(durableId, locNm, location),
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

            foreach (object entry in this.menuDurable.Items)
            {
                ToolStripMenuItem item = entry as ToolStripMenuItem;
                string key = item == null ? null : item.Tag as string;

                if (key != null && DurableManagementPresenter.IsActionKey(key))
                {
                    ApplyActionReason(item, this.ActionReason(key, durable.Row));
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
