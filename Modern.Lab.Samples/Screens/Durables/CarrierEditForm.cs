using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting;
using Modern.Lab.Controls.Wpf.Display;
using Modern.Lab.Data;
using Modern.Lab.Samples.Contracts;
using Modern.Lab.Samples.Services;

using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.Samples
{

    public partial class CarrierEditForm : ModernFormBase
    {
        private DataTable sourceData;
        private DataTable targetData;

        private bool loadingLists;

        private readonly string initialType;
        private readonly string initialSourceId;
        private readonly CarrierEditEntryMode entryMode;

        private const string channelCarriers = "carriers";
        private const string channelSourceMap = "sourceMap";
        private const string channelTargetMap = "targetMap";

        private DataTable stagedWafers;

        private bool carrierListLoading;

        private string loadedType = string.Empty;

        private string loadedSourceId = string.Empty;

        private string loadedTargetId = string.Empty;

        private static string LotColor(string lotId)
        {
            return string.IsNullOrEmpty(lotId)
                    ? string.Empty
                    : Modern.Lab.Theming.Palette.FromValue(lotId);
        }

        public CarrierEditForm()
                : this(string.Empty, string.Empty, CarrierEditEntryMode.Standalone)
        {
        }

        public CarrierEditForm(string initialType, string initialSourceId)
                : this(initialType, initialSourceId, CarrierEditEntryMode.DurableEdit)
        {
        }

        public CarrierEditForm(string initialType, string initialSourceId, CarrierEditEntryMode entryMode)
        {
            this.initialType = (initialType ?? string.Empty).Trim();
            this.initialSourceId = (initialSourceId ?? string.Empty).Trim();
            this.entryMode = entryMode;
            this.InitializeComponent();

            this.cboType.Enabled = !this.SourceLocked;
            this.cboSource.Enabled = !this.SourceLocked;
            this.cboTarget.Enabled = true;

            if (this.IsExchange)
            {
                this.ApplyExchangeChrome();
            }

            this.InitializeModernForm(this.midPanel, false);
        }

        private void ApplyExchangeChrome()
        {
            this.Text = "Carrier Exchange";
        }

        private bool SourceLocked
        {
            get { return this.entryMode != CarrierEditEntryMode.Standalone; }
        }

        private bool IsExchange
        {
            get { return this.entryMode == CarrierEditEntryMode.DurableExchange; }
        }

        private bool ExchangeNeedsFoup
        {
            get
            {
                return this.IsExchange
                        && !string.Equals(
                                this.initialType,
                                ServerFields.Carrier.Foup,
                                StringComparison.Ordinal);
            }
        }

        internal bool SourceUnavailable { get; private set; }

        internal bool ExchangeUnavailable { get; private set; }

        internal bool DurableChanged { get; private set; }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (this.ExchangeNeedsFoup)
            {
                this.ExchangeUnavailable = true;
                this.ShowMessage(
                        ModernMessageKind.Warning,
                        "Carrier Exchange",
                        "Exchange moves a whole FOUP. Use Edit instead.");
                this.Close();
                return;
            }

            DataTable typeTable = new DataTable();
            typeTable.Columns.Add("VALUE", typeof(string));
            typeTable.Rows.Add(ServerFields.Carrier.Foup);
            typeTable.Rows.Add(ServerFields.Carrier.Tray);

            this.cboSource.ItemColorPath = "STAT_COLOR";
            this.cboTarget.ItemColorPath = "STAT_COLOR";

            this.loadingLists = true;

            try
            {
                this.cboType.DisplayMember = "VALUE";
                this.cboType.ValueMember = "VALUE";
                this.cboType.DataSource = typeTable;

                if (this.initialType == ServerFields.Carrier.Foup
                        || this.initialType == ServerFields.Carrier.Tray)
                {
                    this.cboType.SelectedValue = this.initialType;
                }
            }
            finally
            {
                this.loadingLists = false;
            }

            if (this.GetSelectedType().Length == 0)
            {
                this.ReleaseLoadCover();
                return;
            }

            string sourceId = this.GetSelectedType() == this.initialType
                    ? this.initialSourceId
                    : string.Empty;
            this.ReloadCarrierLists(sourceId, null);
        }

        private string GetSelectedType()
        {
            string value = this.cboType.SelectedValue as string;
            return value ?? string.Empty;
        }

        private void OnTypeChanged(object sender, EventArgs e)
        {
            if (this.loadingLists || this.GetSelectedType().Length == 0)
            {
                return;
            }

            if (string.Equals(this.GetSelectedType(), this.loadedType, StringComparison.Ordinal))
            {
                return;
            }

            this.DiscardCarrierState();
            this.ReloadCarrierLists(null, null);
        }

        private void DiscardCarrierState()
        {
            this.InvalidateChannel(channelSourceMap);
            this.InvalidateChannel(channelTargetMap);
            this.ResetActionContractState();

            this.sourceData = null;
            this.targetData = null;

            this.loadingLists = true;

            try
            {
                this.cboSource.DataSource = null;
                this.cboTarget.DataSource = null;
            }
            finally
            {
                this.loadingLists = false;
            }

            this.mapSource.SetSections(null);
            this.mapTarget.SetSections(null);

            this.sourceCard.Text = BuildCardTitle("Source", string.Empty);
            this.targetCard.Text = BuildCardTitle("Target", string.Empty);
            this.sourceCard.TitleRightText = string.Empty;
            this.targetCard.TitleRightText = string.Empty;

            ApplyOccupancy(this.barSource, null);
            ApplyOccupancy(this.barTarget, null);
            this.UpdateItemBadge(this.badgeSourceLot, null);
            this.UpdateItemBadge(this.badgeTargetLot, null);
            this.PositionTitleBadge(this.sourceCard, this.badgeSourceLot);
            this.PositionTitleBadge(this.targetCard, this.badgeTargetLot);

            this.ClearPreview();
        }

        private void OnSourceChanged(object sender, EventArgs e)
        {
            if (this.loadingLists)
            {
                return;
            }

            string selected = this.cboSource.SelectedValue as string ?? string.Empty;

            if (string.Equals(selected, this.loadedSourceId, StringComparison.Ordinal))
            {
                return;
            }

            this.ReloadCarrierLists(selected, null);
        }

        private void OnTargetChanged(object sender, EventArgs e)
        {
            if (this.loadingLists)
            {
                return;
            }

            string selected = this.cboTarget.SelectedValue as string ?? string.Empty;

            if (string.Equals(selected, this.loadedTargetId, StringComparison.Ordinal))
            {
                return;
            }

            this.LoadTargetMap();
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            this.ReloadCarrierLists(
                    this.SourceLocked ? this.initialSourceId : this.cboSource.SelectedValue as string,
                    this.cboTarget.SelectedValue as string);
        }

        private void ReloadCarrierLists(string keepSourceId, string keepTargetId)
        {
            this.ReloadCarrierLists(keepSourceId, keepTargetId, null);
        }

        private async void ReloadCarrierLists(string keepSourceId, string keepTargetId, MethodInvoker onComplete)
        {
            string type = this.GetSelectedType();

            if (type.Length == 0)
            {
                this.ReleaseLoadCover();
                return;
            }

            if (this.carrierListLoading)
            {
                return;
            }

            this.carrierListLoading = true;
            this.loadedType = type;

            this.DiscardCarrierState();
            this.BeginCarrierListContractLoad(type);

            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelCarriers,
                    new Func<DataTable>(delegate { return GetCarriers(type); }));

            this.carrierListLoading = false;

            if (!outcome.IsCurrent)
            {
                return;
            }

            if (outcome.Failure != null)
            {
                this.SetCarrierFailure(type, outcome.Failure);
                this.ReleaseLoadCover();
                this.UpdateActionStates();
                return;
            }

            this.ApplyCarrierLists(outcome.Value, type, keepSourceId, keepTargetId, onComplete);
        }

        private void ApplyCarrierLists(
                DataTable carriers, string type, string keepSourceId, string keepTargetId,
                MethodInvoker onComplete)
        {
            carriers = this.BindCarrierResponse(carriers, type);
            carriers.Columns.Add("LABEL", typeof(string));
            carriers.Columns.Add("STAT_COLOR", typeof(string));

            foreach (DataRow row in carriers.Rows)
            {
                string durableId = TableHelper.CellText(row, ServerFields.Durable.DurableId);
                int useNumcnt = TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Durable.UseNumcnt));
                int capa = TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Durable.Capa));

                if (type == ServerFields.Carrier.Foup)
                {
                    int useFoupCount = TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.UseFoupCount));
                    int foupCapa = TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.FoupCapa));
                    row["LABEL"] = durableId + " · "
                            + WithCapacity(useFoupCount, foupCapa);
                }
                else
                {
                    int stub = TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.UseStubCount));
                    int lcc = TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.UseLccCount));
                    int stubCapa = TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.StubCapa));
                    int lccCapa = TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.LccCapa));
                    row["LABEL"] = durableId
                            + " · " + ServerFields.Lot.SubProdTyp.Chip + " "
                            + WithCapacity(stub, stubCapa)
                            + " · " + ServerFields.Lot.SubProdTyp.Lamella + " "
                            + WithCapacity(lcc, lccCapa);
                }

                double ratio = capa > 0 ? (double)useNumcnt / capa : 0d;

                if (useNumcnt == 0)
                {
                    row["STAT_COLOR"] = "#64748B";
                }
                else if (useNumcnt >= capa)
                {
                    row["STAT_COLOR"] = "#DC2626";
                }
                else if (ratio >= 0.75d)
                {
                    row["STAT_COLOR"] = "#D97706";
                }
                else
                {
                    row["STAT_COLOR"] = "#16A34A";
                }
            }

            this.loadingLists = true;

            try
            {
                this.cboSource.DisplayMember = "LABEL";
                this.cboSource.ValueMember = ServerFields.Durable.DurableId;
                DataTable sources = carriers.Clone();
                foreach (DataRow row in carriers.Rows)
                {
                    if (CarrierEditPresenter.IsSourceCandidate(row))
                    {
                        sources.ImportRow(row);
                    }
                }

                this.cboSource.DataSource = sources;

                if (!string.IsNullOrEmpty(keepSourceId))
                {
                    this.cboSource.SelectedValue = keepSourceId;
                }

                DataRowView selectedSource = this.cboSource.SelectedItem as DataRowView;
                string sourceId = selectedSource == null
                        ? string.Empty
                        : TableHelper.CellText(selectedSource.Row, ServerFields.Durable.DurableId);

                this.loadedSourceId = sourceId;

                if (this.SourceLocked
                        && !string.Equals(sourceId, this.initialSourceId, StringComparison.Ordinal))
                {
                    this.cboSource.SelectedValue = null;
                    this.cboTarget.DataSource = null;
                    this.SourceUnavailable = true;
                }
                else
                {
                    DataTable targets = CarrierEditPresenter.BuildTargetCandidates(
                            carriers,
                            sourceId,
                            this.IsExchange
                                    ? (selectedSource == null ? null : selectedSource.Row)
                                    : null);

                    this.cboTarget.DisplayMember = "LABEL";
                    this.cboTarget.ValueMember = ServerFields.Durable.DurableId;
                    this.cboTarget.DataSource = targets;
                    this.loadedTargetId = this.cboTarget.SelectedValue as string ?? string.Empty;

                    if (!string.IsNullOrEmpty(keepTargetId))
                    {
                        this.cboTarget.SelectedValue = keepTargetId;
                        this.loadedTargetId = this.cboTarget.SelectedValue as string ?? string.Empty;
                    }
                }
            }
            finally
            {
                this.loadingLists = false;
            }

            if (this.SourceUnavailable)
            {
                this.ReleaseLoadCover();
                this.UpdateActionStates();
                this.ShowMessage(
                        ModernMessageKind.Warning,
                        "Carrier Edit",
                        "Durable Id " + this.initialSourceId
                                + " is no longer available. Refresh Durable Management and try again.");
                this.Close();
                return;
            }

            this.LoadMaps(new MethodInvoker(delegate
            {
                this.ClearPreview();

                if (onComplete != null)
                {
                    onComplete();
                }
            }));
        }

        private MethodInvoker pendingMapsDone;
        private bool sourceMapSettled;
        private bool targetMapSettled;

        private void LoadMaps(MethodInvoker onBothApplied)
        {
            this.sourceMapSettled = false;
            this.targetMapSettled = false;
            this.pendingMapsDone = new MethodInvoker(delegate
            {
                this.ReleaseLoadCover();

                if (onBothApplied != null)
                {
                    onBothApplied();
                }
            });

            this.LoadSourceMap();
            this.LoadTargetMap();
        }

        private void OnMapSettled(bool sourceSide, bool current)
        {
            if (!current)
            {
                return;
            }

            if (sourceSide)
            {
                this.sourceMapSettled = true;
            }
            else
            {
                this.targetMapSettled = true;
            }

            if (this.pendingMapsDone != null && this.sourceMapSettled && this.targetMapSettled)
            {
                MethodInvoker done = this.pendingMapsDone;
                this.pendingMapsDone = null;
                done();
            }

            this.RebuildExchangeStaging();
        }

        private void LoadSourceMap()
        {
            this.LoadSourceMap(null);
        }

        private async void LoadSourceMap(MethodInvoker onApplied)
        {
            string type = this.GetSelectedType();
            string carrierId = this.cboSource.SelectedValue as string ?? string.Empty;

            this.loadedSourceId = carrierId;
            this.ClearSourceMapState();
            this.BeginSourceMapContractLoad(type, carrierId);

            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelSourceMap,
                    new Func<DataTable>(delegate { return GetDurableWafers(carrierId); }));

            if (!outcome.IsCurrent)
            {
                this.OnMapSettled(true, false);
                return;
            }

            if (outcome.Failure != null)
            {
                this.SetMapFailure(CarrierEditContracts.SourceMapTable, type, carrierId, outcome.Failure);
                this.UpdateActionStates();
                this.OnMapSettled(true, true);
                return;
            }

            this.sourceData = this.BindMapResponse(
                    CarrierEditContracts.SourceMapTable, outcome.Value, type, carrierId, this.cboSource);
            this.mapSource.SetSections(BuildSections(this.sourceData, type));

            this.sourceCard.Text = BuildCardTitle("Source", carrierId);
            this.sourceCard.TitleRightText = BuildFilledText(this.cboSource, type);
            ApplyOccupancy(this.barSource, this.cboSource);
            this.UpdateItemBadge(this.badgeSourceLot, this.sourceData);
            this.PositionTitleBadge(this.sourceCard, this.badgeSourceLot);
            this.UpdateActionStates();
            this.OnMapSettled(true, true);

            if (onApplied != null)
            {
                onApplied();
            }
        }

        private void LoadTargetMap()
        {
            this.LoadTargetMap(null);
        }

        private async void LoadTargetMap(MethodInvoker onApplied)
        {
            string type = this.GetSelectedType();
            string carrierId = this.cboTarget.SelectedValue as string ?? string.Empty;

            this.loadedTargetId = carrierId;
            this.ClearTargetMapState();
            this.BeginTargetMapContractLoad(type, carrierId);

            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelTargetMap,
                    new Func<DataTable>(delegate { return GetDurableWafers(carrierId); }));

            if (!outcome.IsCurrent)
            {
                this.OnMapSettled(false, false);
                return;
            }

            if (outcome.Failure != null)
            {
                this.SetMapFailure(CarrierEditContracts.TargetMapTable, type, carrierId, outcome.Failure);
                this.UpdateActionStates();
                this.OnMapSettled(false, true);
                return;
            }

            this.targetData = this.BindMapResponse(
                    CarrierEditContracts.TargetMapTable, outcome.Value, type, carrierId, this.cboTarget);
            this.mapTarget.SetSections(BuildSections(this.targetData, type));

            this.targetCard.Text = BuildCardTitle("Target", carrierId);
            this.targetCard.TitleRightText = BuildFilledText(this.cboTarget, type);
            ApplyOccupancy(this.barTarget, this.cboTarget);
            this.UpdateItemBadge(this.badgeTargetLot, this.targetData);
            this.PositionTitleBadge(this.targetCard, this.badgeTargetLot);
            this.UpdateActionStates();
            this.OnMapSettled(false, true);

            if (onApplied != null)
            {
                onApplied();
            }
        }

        private static string ExchangeStatusText(bool sourceHasWafers, int targetFilled, int stagedCount)
        {
            if (!sourceHasWafers)
            {
                return "Source carrier is empty — nothing to exchange.";
            }

            if (targetFilled > 0)
            {
                return "Target carrier is no longer empty — pick an empty carrier.";
            }

            if (stagedCount == 0)
            {
                return "Pick an empty target carrier to stage the whole source.";
            }

            return stagedCount.ToString("N0") + " item(s) staged — the whole source moves as one.";
        }

        private void ClearSourceMapState()
        {
            this.sourceData = null;
            this.ResetSourceMapContractState();
            this.mapSource.SetSections(null);
            this.sourceCard.Text = BuildCardTitle("Source", string.Empty);
            this.sourceCard.TitleRightText = string.Empty;
            ApplyOccupancy(this.barSource, null);
            this.UpdateItemBadge(this.badgeSourceLot, null);
            this.ClearPreview();
        }

        private void ClearTargetMapState()
        {
            this.targetData = null;
            this.ResetTargetMapContractState();
            this.mapTarget.SetSections(null);
            this.targetCard.Text = BuildCardTitle("Target", string.Empty);
            this.targetCard.TitleRightText = string.Empty;
            ApplyOccupancy(this.barTarget, null);
            this.UpdateItemBadge(this.badgeTargetLot, null);
            this.ClearPreview();
        }

        private void UpdateActionStates()
        {
            int targetFilled = CarrierEditPresenter.CountFilled(this.targetData);
            int targetCap = this.targetData != null ? this.targetData.Rows.Count : 0;
            bool hasTarget = this.TargetId().Length > 0 && targetCap > 0;
            bool sourceHasWafers = CarrierEditPresenter.CountFilled(this.sourceData) > 0;
            bool hasUnstagedSourceWafers = this.HasUnstagedSourceWafers();
            bool selectedUnstaged = this.HasSelectedSourceKeys(false);
            bool selectedTargetPreview = this.selectedTargetPreviewKeys.Count > 0;
            bool anyStaged = this.stagedKeys.Count > 0;
            bool hasStagedWafers = this.stagedWafers != null && this.stagedWafers.Rows.Count > 0;
            int stagedCount = hasStagedWafers ? this.stagedWafers.Rows.Count : 0;
            bool moveAllowed = this.ActionShapeAllows(CarrierEditPresenter.ActionMove)
                    && CarrierEditPresenter.CanExecute(
                            CarrierEditPresenter.ActionMove,
                            this.GetSelectedType(),
                            this.SourceId(),
                            this.TargetId(),
                            this.sourceData,
                            this.targetData,
                            this.stagedWafers);
            bool scrapAllowed = !this.SourceLocked
                    && this.ActionShapeAllows(CarrierEditPresenter.ActionScrap)
                    && CarrierEditPresenter.CanExecute(
                            CarrierEditPresenter.ActionScrap,
                            this.GetSelectedType(),
                            this.SourceId(),
                            this.TargetId(),
                            this.sourceData,
                            this.targetData,
                            this.stagedWafers);

            bool selRight = !this.IsExchange && selectedUnstaged;
            bool allRight = !this.IsExchange && sourceHasWafers && hasUnstagedSourceWafers;
            bool selLeft = !this.IsExchange && selectedTargetPreview;
            bool allLeft = !this.IsExchange && anyStaged;

            this.btnSelRight.Enabled = selRight;
            this.btnAllRight.Enabled = allRight;
            this.btnSelLeft.Enabled = selLeft;
            this.btnAllLeft.Enabled = allLeft;

            this.miMoveSelRight.Enabled = selRight;
            this.miMoveAllRight.Enabled = allRight;
            this.miMoveSelLeft.Enabled = selLeft;
            this.miMoveAllLeft.Enabled = allLeft;

            this.btnMove.Enabled = moveAllowed;
            this.btnScrap.Enabled = scrapAllowed;

            this.targetCard.TitleAccent = anyStaged;
            this.UpdateTransferPlanText(stagedCount);
            this.btnMove.Kind = this.btnMove.Enabled
                    ? Modern.Lab.Controls.Wpf.Input.ButtonKind.Primary
                    : Modern.Lab.Controls.Wpf.Input.ButtonKind.Secondary;
            this.btnMove.Text = this.IsExchange ? "Exchange" : "Move";

            if (this.stageCapacityReason.Length > 0)
            {
                this.lblActionStatus.Text = this.stageCapacityReason;
            }
            else if (this.IsExchange)
            {
                this.lblActionStatus.Text = ExchangeStatusText(sourceHasWafers, targetFilled, stagedCount);
            }
            else if (selectedTargetPreview)
            {
                this.lblActionStatus.Text = this.selectedTargetPreviewKeys.Count.ToString("N0")
                        + " target previews selected · Press ‹ to return.";
            }
            else if (stagedCount == 0)
            {
                this.lblActionStatus.Text = "Select one or more source wafers, then stage with › or ».";
            }
            else if (!hasTarget)
            {
                this.lblActionStatus.Text = stagedCount.ToString("N0")
                        + " wafers staged · Select a target carrier to continue.";
            }
            else if (!moveAllowed)
            {
                this.lblActionStatus.Text = this.CarrierActionReason(CarrierEditPresenter.ActionMove);
            }
            else
            {
                this.lblActionStatus.Text = stagedCount.ToString("N0") + " wafers staged · Target has "
                        + (targetCap - targetFilled).ToString("N0") + " open positions.";
            }
        }

        private bool HasUnstagedSourceWafers()
        {
            if (this.sourceData == null)
            {
                return false;
            }

            foreach (DataRow row in this.sourceData.Rows)
            {
                string waferId = TableHelper.CellText(row, ServerFields.Unit.WfId);

                if (waferId.Length == 0)
                {
                    continue;
                }

                string key = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column)
                        + "|" + TableHelper.CellText(row, ServerFields.Unit.SlotNo);

                if (!this.stagedKeys.Contains(key))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateTransferPlanText(int stagedCount)
        {
            if (this.GetSelectedType() != ServerFields.Carrier.Tray)
            {
                this.lblTransfer.Text = "PLAN";
                this.lblTransferHint.Text = stagedCount > 0
                        ? stagedCount.ToString("N0") + " staged"
                        : "stage wafers";
                return;
            }

            int stubCount = 0;
            int lccCount = 0;

            if (this.stagedWafers != null)
            {
                foreach (DataRow row in this.stagedWafers.Rows)
                {
                    string kind = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column);

                    if (kind == ServerFields.Lot.SubProdTyp.Chip)
                    {
                        stubCount++;
                    }
                    else if (kind == ServerFields.Lot.SubProdTyp.Lamella)
                    {
                        lccCount++;
                    }
                }
            }

            this.lblTransfer.Text = "Chip / Lamella";
            this.lblTransferHint.Text = stubCount.ToString("N0") + " / "
                    + lccCount.ToString("N0") + " staged";
        }

        private static string BuildCardTitle(string role, string carrierId)
        {
            if (carrierId.Length == 0)
            {
                return role;
            }

            return role + " — " + carrierId;
        }

        private static string WithCapacity(int filled, int capacity)
        {
            return capacity > 0
                    ? filled.ToString("N0") + "/" + capacity.ToString("N0")
                    : filled.ToString("N0");
        }

        private static string BuildFilledText(
                Modern.Lab.WinForms.Controls.Selection.ModernComboBox combo,
                string type)
        {
            DataRowView selected = combo == null ? null : combo.SelectedItem as DataRowView;

            if (selected == null)
            {
                return string.Empty;
            }

            if (type == ServerFields.Carrier.Foup)
            {
                return WithCapacity(
                        TableHelper.ParseInt(TableHelper.CellText(
                                selected.Row, ServerFields.Carrier.UseFoupCount)),
                        TableHelper.ParseInt(TableHelper.CellText(
                                selected.Row, ServerFields.Carrier.FoupCapa)));
            }

            return ServerFields.Lot.SubProdTyp.Chip + " "
                    + WithCapacity(
                            TableHelper.ParseInt(TableHelper.CellText(
                                    selected.Row, ServerFields.Carrier.UseStubCount)),
                            TableHelper.ParseInt(TableHelper.CellText(
                                    selected.Row, ServerFields.Carrier.StubCapa)))
                    + "   ·   " + ServerFields.Lot.SubProdTyp.Lamella + " "
                    + WithCapacity(
                            TableHelper.ParseInt(TableHelper.CellText(
                                    selected.Row, ServerFields.Carrier.UseLccCount)),
                            TableHelper.ParseInt(TableHelper.CellText(
                                    selected.Row, ServerFields.Carrier.LccCapa)));
        }

        private static void ApplyOccupancy(
                Modern.Lab.WinForms.Controls.Display.ModernProgressBar bar,
                Modern.Lab.WinForms.Controls.Selection.ModernComboBox combo)
        {
            DataRowView selected = combo == null ? null : combo.SelectedItem as DataRowView;
            int total = selected == null
                    ? 0
                    : TableHelper.ParseInt(TableHelper.CellText(
                            selected.Row, ServerFields.Durable.Capa));
            int used = selected == null
                    ? 0
                    : TableHelper.ParseInt(TableHelper.CellText(
                            selected.Row, ServerFields.Durable.UseNumcnt));

            bar.Minimum = 0;
            bar.Maximum = total;
            bar.Value = used;
        }

        private void PositionTitleBadge(
                Modern.Lab.WinForms.Controls.Layout.ModernGroupBox card,
                Modern.Lab.WinForms.Controls.Display.ModernStatusBadge badge)
        {
            string title = card.Text ?? string.Empty;
            int x = 12;

            using (System.Drawing.Graphics g = this.CreateGraphics())
            using (System.Drawing.Font font =
                    new System.Drawing.Font("Segoe UI Semibold", card.TitleFontSize))
            {
                double ratio = Modern.Lab.Theming.ModernTheme.ResolveFontWidthRatio(card.FontWidthRatio);
                float width = (float)(g.MeasureString(title, font).Width * ratio);
                x = 12 + (int)System.Math.Ceiling(width) + 8;
            }

            badge.Location = new System.Drawing.Point(x, 4);
        }

        private void UpdateItemBadge(
                Modern.Lab.WinForms.Controls.Display.ModernStatusBadge badge, DataTable wafers)
        {
            if (wafers == null)
            {
                badge.Text = string.Empty;
                badge.Color = string.Empty;
                badge.Visible = false;
                this.tipLots.SetToolTip(badge, null);
                return;
            }

            badge.Visible = true;

            System.Collections.Generic.List<string> lots =
                    new System.Collections.Generic.List<string>();

            {
                foreach (DataRow row in wafers.Rows)
                {
                    string lotId = TableHelper.CellText(row, ServerFields.Lot.LotId).Trim();

                    if (lotId.Length > 0 && !lots.Contains(lotId))
                    {
                        lots.Add(lotId);
                    }
                }
            }

            if (lots.Count == 0)
            {
                badge.Text = "Empty";
                badge.Color = string.Empty;
            }
            else if (lots.Count == 1)
            {
                badge.Text = lots[0];
                badge.Color = LotColor(lots[0]);
            }
            else
            {
                badge.Text = lots[0] + " +" + (lots.Count - 1);
                badge.Color = LotColor(lots[0]);
            }

            this.tipLots.SetToolTip(
                    badge,
                    lots.Count > 1 ? string.Join(", ", lots.ToArray()) : null);
        }

        private SlotMapSection[] BuildSections(DataTable wafers, string type)
        {
            if (type == ServerFields.Carrier.Foup)
            {
                SlotMapSection slots = new SlotMapSection();
                slots.Title = "Slots";
                slots.Columns = 1;
                slots.Kind = SlotMapSectionKind.WaferEdge;

                foreach (DataRow row in wafers.Rows)
                {
                    SlotMapCell cell = new SlotMapCell();
                    cell.Key = ServerFields.Lot.SubProdTyp.Wafer + "|"
                            + TableHelper.CellText(row, ServerFields.Unit.SlotNo);
                    cell.Label = TableHelper.CellText(row, ServerFields.Unit.SlotNo);
                    cell.WaferId = TableHelper.CellText(row, ServerFields.Unit.WfId);

                    string lotId = TableHelper.CellText(row, ServerFields.Lot.LotId);

                    if (lotId.Length > 0)
                    {
                        cell.Color = LotColor(lotId);
                        cell.ToolTip = cell.WaferId + " — " + lotId;
                    }

                    slots.Cells.Add(cell);
                }

                return new SlotMapSection[] { slots };
            }

            SlotMapSection stubs = new SlotMapSection();
            stubs.Title = ServerFields.Lot.SubProdTyp.Chip;
            stubs.Columns = 3;
            stubs.CellFontSize = 12d;
            stubs.Kind = SlotMapSectionKind.PinStub;

            SlotMapSection lccs = new SlotMapSection();
            lccs.Title = ServerFields.Lot.SubProdTyp.Lamella;
            lccs.Columns = 5;
            lccs.Kind = SlotMapSectionKind.LamellaPost;

            SlotMapCell current = null;

            foreach (DataRow row in wafers.Rows)
            {
                string kind = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column);
                string pos = TableHelper.CellText(row, ServerFields.Unit.SlotNo);
                string lotId = TableHelper.CellText(row, ServerFields.Lot.LotId);

                if (kind == ServerFields.Lot.SubProdTyp.Chip)
                {
                    SlotMapCell stub = new SlotMapCell();
                    stub.Key = ServerFields.Lot.SubProdTyp.Chip + "|" + pos;
                    stub.Label = pos;
                    stub.WaferId = TableHelper.CellText(row, ServerFields.Unit.WfId);

                    if (lotId.Length > 0)
                    {
                        stub.Color = LotColor(lotId);
                        stub.ToolTip = stub.WaferId + " — " + lotId;
                    }

                    stubs.Cells.Add(stub);
                    continue;
                }

                if (current == null || current.Key != ServerFields.Lot.SubProdTyp.Lamella + "|" + pos)
                {
                    current = new SlotMapCell();
                    current.Key = ServerFields.Lot.SubProdTyp.Lamella + "|" + pos;
                    current.Label = pos;
                    current.SubCells = new System.Collections.Generic.List<SlotMapSubCell>();
                    lccs.Cells.Add(current);
                }

                SlotMapSubCell finger = new SlotMapSubCell();
                finger.Name = TableHelper.CellText(row, ServerFields.Unit.FingerId);
                finger.WaferId = TableHelper.CellText(row, ServerFields.Unit.WfId);
                finger.Marker = TableHelper.CellText(row, ServerFields.Unit.FingerIndex);

                if (lotId.Length > 0)
                {
                    finger.Color = LotColor(lotId);
                    finger.Detail = lotId;
                }

                current.SubCells.Add(finger);
            }

            return new SlotMapSection[] { stubs, lccs };
        }

        private static int ReadCapa(
                Modern.Lab.WinForms.Controls.Selection.ModernComboBox combo, string column)
        {
            DataRowView row = combo.SelectedItem as DataRowView;
            return row == null ? 0 : TableHelper.ParseInt(TableHelper.CellText(row.Row, column));
        }

        private DataTable NormalizeWafers(
                DataTable raw, Modern.Lab.WinForms.Controls.Selection.ModernComboBox combo)
        {
            return SlotMapWaferTable.Normalize(
                    raw,
                    ReadCapa(combo, ServerFields.Carrier.FoupCapa),
                    ReadCapa(combo, ServerFields.Carrier.StubCapa),
                    ReadCapa(combo, ServerFields.Carrier.LccCapa),
                    0,
                    0,
                    0);
        }

        private readonly List<string> stagedKeys = new List<string>();
        private readonly List<string> selectedSourceKeys = new List<string>();
        private readonly List<string> selectedTargetPreviewKeys = new List<string>();
        private readonly Dictionary<string, string> previewSourceKeyByTargetKey =
                new Dictionary<string, string>(StringComparer.Ordinal);
        private string stageCapacityReason = string.Empty;

        private void OnSourceCellClicked(object sender, SlotMapCellEventArgs e)
        {
            if (this.IsExchange)
            {
                return;
            }

            if (!this.SourceKeyHasWafer(e.Key))
            {
                return;
            }

            if (this.stagedKeys.Contains(e.Key))
            {
                return;
            }

            this.stageCapacityReason = string.Empty;
            this.selectedTargetPreviewKeys.Clear();

            if (this.selectedSourceKeys.Contains(e.Key))
            {
                this.selectedSourceKeys.Remove(e.Key);
            }
            else
            {
                this.selectedSourceKeys.Add(e.Key);
            }

            this.RenderSelections();
        }

        private bool SourceKeyHasWafer(string key)
        {
            if (this.sourceData == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            foreach (DataRow row in this.sourceData.Rows)
            {
                string rowKey = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column)
                        + "|" + TableHelper.CellText(row, ServerFields.Unit.SlotNo);

                if (string.Equals(rowKey, key, StringComparison.Ordinal)
                        && TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasSelectedSourceKeys(bool staged)
        {
            foreach (string key in this.selectedSourceKeys)
            {
                if (this.stagedKeys.Contains(key) == staged)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnTargetCellClicked(object sender, SlotMapCellEventArgs e)
        {
            if (this.IsExchange)
            {
                return;
            }

            if (!this.previewSourceKeyByTargetKey.ContainsKey(e.Key))
            {
                return;
            }

            this.stageCapacityReason = string.Empty;
            this.selectedSourceKeys.Clear();

            if (this.selectedTargetPreviewKeys.Contains(e.Key))
            {
                this.selectedTargetPreviewKeys.Remove(e.Key);
            }
            else
            {
                this.selectedTargetPreviewKeys.Add(e.Key);
            }

            this.RenderSelections();
        }

        private void OnSourceCellRightClick(object sender, SlotMapCellEventArgs e)
        {
            if (this.SourceKeyHasWafer(e.Key))
            {
                this.stageCapacityReason = string.Empty;
                this.selectedTargetPreviewKeys.Clear();
                this.selectedSourceKeys.Clear();
                this.selectedSourceKeys.Add(e.Key);
                this.RenderSelections();
            }

            this.moveMenu.Show(System.Windows.Forms.Cursor.Position);
        }

        private void OnTargetCellRightClick(object sender, SlotMapCellEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Key))
            {
                this.OnTargetCellClicked(sender, e);
            }

            this.moveMenu.Show(System.Windows.Forms.Cursor.Position);
        }

        private void RebuildStagedPreview()
        {
            if (this.stagedKeys.Count == 0)
            {
                this.stagedWafers = null;
                this.previewSourceKeyByTargetKey.Clear();
                this.selectedTargetPreviewKeys.Clear();
                this.mapTarget.SetPreview(null);
                this.mapTarget.SetPreviewMarkers(null);
                return;
            }

            this.stagedWafers = this.CollectStagedWafers();

            Dictionary<string, string> preview = this.BuildLocalPreview(this.stagedWafers);
            this.mapTarget.SetPreview(preview);
            this.mapTarget.SetPreviewMarkers(this.BuildPreviewMarkers(this.stagedWafers, preview));

            for (int index = this.selectedTargetPreviewKeys.Count - 1; index >= 0; index--)
            {
                if (!this.previewSourceKeyByTargetKey.ContainsKey(this.selectedTargetPreviewKeys[index]))
                {
                    this.selectedTargetPreviewKeys.RemoveAt(index);
                }
            }
        }

        private Dictionary<string, string> BuildMirrorPreview(
                DataTable staged, Dictionary<string, string> preview)
        {
            foreach (DataRow row in staged.Rows)
            {
                string waferId = TableHelper.CellText(row, ServerFields.Unit.WfId).Trim();

                if (waferId.Length == 0)
                {
                    continue;
                }

                string kind = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column);
                string position = TableHelper.CellText(row, ServerFields.Unit.SlotNo);
                string key = kind + "|" + position;

                if (kind == ServerFields.Lot.SubProdTyp.Lamella)
                {
                    key = key + "|" + TableHelper.CellText(row, ServerFields.Unit.FingerId);
                }

                preview[key] = waferId;
                this.previewSourceKeyByTargetKey[key] = kind + "|" + position;
            }

            return preview;
        }

        private void RebuildExchangeStaging()
        {
            if (!this.IsExchange)
            {
                return;
            }

            List<string> proposed = this.CollectFilledSourceKeys();
            this.stageCapacityReason = string.Empty;

            if (proposed.Count > 0)
            {
                if (this.targetData == null || this.TargetId().Length == 0)
                {
                    proposed.Clear();
                }
                else
                {
                    this.stageCapacityReason = CarrierEditPresenter.StagingCapacityReason(
                            this.GetSelectedType(), this.targetData, this.CollectWafers(proposed));

                    if (this.stageCapacityReason.Length > 0)
                    {
                        proposed.Clear();
                    }
                }
            }

            if (SameKeys(this.stagedKeys, proposed))
            {
                return;
            }

            this.stagedKeys.Clear();
            this.stagedKeys.AddRange(proposed);
            this.selectedSourceKeys.Clear();
            this.selectedTargetPreviewKeys.Clear();
            this.RebuildStagedPreview();
            this.RenderSelections();
        }

        private List<string> CollectFilledSourceKeys()
        {
            List<string> keys = new List<string>();

            if (this.sourceData == null)
            {
                return keys;
            }

            foreach (DataRow row in this.sourceData.Rows)
            {
                if (TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length == 0)
                {
                    continue;
                }

                string key = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column)
                        + "|" + TableHelper.CellText(row, ServerFields.Unit.SlotNo);

                if (!keys.Contains(key))
                {
                    keys.Add(key);
                }
            }

            return keys;
        }

        private static bool SameKeys(List<string> first, List<string> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            foreach (string key in first)
            {
                if (!second.Contains(key))
                {
                    return false;
                }
            }

            return true;
        }

        private Dictionary<string, string> BuildLocalPreview(DataTable staged)
        {
            Dictionary<string, string> preview =
                    new Dictionary<string, string>(StringComparer.Ordinal);
            this.previewSourceKeyByTargetKey.Clear();

            if (staged == null || staged.Rows.Count == 0
                    || this.targetData == null || this.TargetId().Length == 0)
            {
                return preview;
            }

            if (this.IsExchange)
            {
                return this.BuildMirrorPreview(staged, preview);
            }

            List<string> emptySlots = new List<string>();
            List<string> emptyStubs = new List<string>();
            List<string> lccOrder = new List<string>();
            Dictionary<string, bool> lccEmpty =
                    new Dictionary<string, bool>(StringComparer.Ordinal);

            foreach (DataRow row in this.targetData.Rows)
            {
                string kind = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column);
                string pos = TableHelper.CellText(row, ServerFields.Unit.SlotNo);
                bool filled = TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length > 0;

                if (kind == ServerFields.Lot.SubProdTyp.Lamella)
                {
                    if (!lccEmpty.ContainsKey(pos))
                    {
                        lccEmpty[pos] = true;
                        lccOrder.Add(pos);
                    }

                    if (filled)
                    {
                        lccEmpty[pos] = false;
                    }
                }
                else if (!filled)
                {
                    if (kind == ServerFields.Lot.SubProdTyp.Chip)
                    {
                        emptyStubs.Add(pos);
                    }
                    else
                    {
                        emptySlots.Add(pos);
                    }
                }
            }

            List<string> emptyLccs = new List<string>();

            foreach (string pos in lccOrder)
            {
                if (lccEmpty[pos])
                {
                    emptyLccs.Add(pos);
                }
            }

            int slotNext = 0;
            int stubNext = 0;
            int lccNext = 0;
            Dictionary<string, string> lccAssigned =
                    new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (DataRow row in staged.Rows)
            {
                string kind = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column);
                string waferId = TableHelper.CellText(row, ServerFields.Unit.WfId).Trim();
                string sourcePosition = TableHelper.CellText(row, ServerFields.Unit.SlotNo);
                string sourceKey = kind + "|" + sourcePosition;

                if (waferId.Length == 0)
                {
                    continue;
                }

                if (kind == ServerFields.Lot.SubProdTyp.Chip)
                {
                    if (stubNext < emptyStubs.Count)
                    {
                        string targetKey = ServerFields.Lot.SubProdTyp.Chip + "|" + emptyStubs[stubNext];
                        preview[targetKey] = waferId;
                        this.previewSourceKeyByTargetKey[targetKey] = sourceKey;
                        stubNext = stubNext + 1;
                    }
                }
                else if (kind == ServerFields.Lot.SubProdTyp.Lamella)
                {
                    string tgtPos;

                    if (!lccAssigned.TryGetValue(sourcePosition, out tgtPos))
                    {
                        if (lccNext >= emptyLccs.Count)
                        {
                            continue;
                        }

                        tgtPos = emptyLccs[lccNext];
                        lccNext = lccNext + 1;
                        lccAssigned[sourcePosition] = tgtPos;
                    }

                    preview[ServerFields.Lot.SubProdTyp.Lamella + "|" + tgtPos + "|"
                            + TableHelper.CellText(row, ServerFields.Unit.FingerId)] = waferId;
                    this.previewSourceKeyByTargetKey[
                            ServerFields.Lot.SubProdTyp.Lamella + "|" + tgtPos] = sourceKey;
                }
                else
                {
                    if (slotNext < emptySlots.Count)
                    {
                        string targetKey = ServerFields.Lot.SubProdTyp.Wafer + "|" + emptySlots[slotNext];
                        preview[targetKey] = waferId;
                        this.previewSourceKeyByTargetKey[targetKey] = sourceKey;
                        slotNext = slotNext + 1;
                    }
                }
            }

            return preview;
        }

        private System.Collections.Generic.Dictionary<string, string> BuildPreviewMarkers(
                DataTable wafers, System.Collections.Generic.Dictionary<string, string> preview)
        {
            System.Collections.Generic.Dictionary<string, string> markers =
                    new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
            System.Collections.Generic.Dictionary<string, string> markerByWaferId =
                    new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);

            if (wafers == null || preview == null)
            {
                return markers;
            }

            foreach (DataRow row in wafers.Rows)
            {
                string waferId = TableHelper.CellText(row, ServerFields.Unit.WfId);
                string marker = TableHelper.CellText(row, ServerFields.Unit.FingerIndex);

                if (waferId.Length > 0 && marker.Length > 0)
                {
                    markerByWaferId[waferId] = marker;
                }
            }

            foreach (System.Collections.Generic.KeyValuePair<string, string> entry in preview)
            {
                string marker;

                if (markerByWaferId.TryGetValue(entry.Value, out marker))
                {
                    markers[entry.Key] = marker;
                }
            }

            return markers;
        }

        private void RenderSelections()
        {
            string[] staged = new string[this.stagedKeys.Count];
            this.stagedKeys.CopyTo(staged);
            string[] selected = new string[this.selectedSourceKeys.Count];
            this.selectedSourceKeys.CopyTo(selected);
            string[] selectedTarget = new string[this.selectedTargetPreviewKeys.Count];
            this.selectedTargetPreviewKeys.CopyTo(selectedTarget);
            this.mapSource.SetSelectedKeys(staged);
            this.mapTarget.SetSelectedKeys(new string[0]);

            this.mapSource.SetClickKeys(selected);
            this.mapTarget.SetClickKeys(selectedTarget);

            this.UpdateActionStates();
        }

        private void ClearPreview()
        {
            this.stageCapacityReason = string.Empty;
            this.stagedKeys.Clear();
            this.selectedSourceKeys.Clear();
            this.selectedTargetPreviewKeys.Clear();
            this.previewSourceKeyByTargetKey.Clear();
            this.stagedWafers = null;
            this.mapSource.SetSelectedKeys(new string[0]);
            this.mapTarget.SetSelectedKeys(new string[0]);
            this.mapSource.SetPreview(null);
            this.mapTarget.SetPreview(null);
            this.mapTarget.SetPreviewMarkers(null);
            this.mapSource.SetClickKeys(new string[0]);
            this.mapTarget.SetClickKeys(new string[0]);
            this.UpdateActionStates();
        }

        private DataTable CollectStagedWafers()
        {
            return this.CollectWafers(this.stagedKeys);
        }

        private DataTable CollectWafers(IEnumerable<string> keys)
        {
            DataTable selected = this.sourceData.Clone();

            Dictionary<string, List<DataRow>> byKey =
                    new Dictionary<string, List<DataRow>>(StringComparer.Ordinal);

            foreach (DataRow row in this.sourceData.Rows)
            {
                if (TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length == 0)
                {
                    continue;
                }

                string key = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column)
                        + "|" + TableHelper.CellText(row, ServerFields.Unit.SlotNo);
                List<DataRow> rows;

                if (!byKey.TryGetValue(key, out rows))
                {
                    rows = new List<DataRow>();
                    byKey.Add(key, rows);
                }

                rows.Add(row);
            }

            foreach (string key in keys)
            {
                List<DataRow> rows;

                if (!byKey.TryGetValue(key, out rows))
                {
                    continue;
                }

                foreach (DataRow row in rows)
                {
                    selected.ImportRow(row);
                }
            }

            return selected;
        }

        private DataTable CollectAllWafers(DataTable data)
        {
            DataTable selected = data.Clone();

            foreach (DataRow row in data.Rows)
            {
                if (TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length > 0)
                {
                    selected.ImportRow(row);
                }
            }

            return selected;
        }

        private void OnMoveAllRight(object sender, EventArgs e)
        {
            if (this.IsExchange)
            {
                return;
            }

            if (this.CollectAllWafers(this.sourceData).Rows.Count == 0)
            {
                this.ShowToast("Source carrier is empty.", ToastKind.Warning);
                return;
            }

            List<string> proposedKeys = new List<string>();

            foreach (DataRow row in this.sourceData.Rows)
            {
                if (TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length > 0)
                {
                    string key = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column)
                            + "|" + TableHelper.CellText(row, ServerFields.Unit.SlotNo);

                    if (!proposedKeys.Contains(key))
                    {
                        proposedKeys.Add(key);
                    }
                }
            }

            if (!this.CanApplyStaging(proposedKeys))
            {
                return;
            }

            this.stagedKeys.Clear();
            this.stagedKeys.AddRange(proposedKeys);
            this.selectedSourceKeys.Clear();
            this.selectedTargetPreviewKeys.Clear();

            this.RebuildStagedPreview();
            this.RenderSelections();
        }

        private void OnMoveSelRight(object sender, EventArgs e)
        {
            if (this.IsExchange)
            {
                return;
            }

            if (!this.HasSelectedSourceKeys(false))
            {
                this.ShowToast("Select one or more source wafers first, then →.", ToastKind.Warning);
                return;
            }

            List<string> proposedKeys = new List<string>(this.stagedKeys);

            foreach (string key in this.selectedSourceKeys)
            {
                if (!proposedKeys.Contains(key))
                {
                    proposedKeys.Add(key);
                }
            }

            if (!this.CanApplyStaging(proposedKeys))
            {
                return;
            }

            this.stagedKeys.Clear();
            this.stagedKeys.AddRange(proposedKeys);

            this.selectedSourceKeys.Clear();
            this.selectedTargetPreviewKeys.Clear();
            this.RebuildStagedPreview();
            this.RenderSelections();
        }

        private bool CanApplyStaging(IList<string> proposedKeys)
        {
            this.stageCapacityReason = string.Empty;

            if (this.TargetId().Length == 0 || this.targetData == null
                    || !this.ActionShapeAllows(CarrierEditPresenter.ActionMove))
            {
                return true;
            }

            DataTable proposed = this.CollectWafers(proposedKeys);
            this.stageCapacityReason = CarrierEditPresenter.StagingCapacityReason(
                    this.GetSelectedType(), this.targetData, proposed);

            if (this.stageCapacityReason.Length == 0)
            {
                return true;
            }

            this.UpdateActionStates();
            this.ShowMessage(
                    ModernMessageKind.Warning,
                    "Carrier Editor — Move",
                    this.stageCapacityReason);
            return false;
        }

        private void OnMoveSelLeft(object sender, EventArgs e)
        {
            if (this.IsExchange)
            {
                return;
            }

            if (this.selectedTargetPreviewKeys.Count == 0)
            {
                this.ShowToast("Select one or more target previews first, then ‹.", ToastKind.Warning);
                return;
            }

            this.stageCapacityReason = string.Empty;

            foreach (string targetKey in this.selectedTargetPreviewKeys)
            {
                string sourceKey;

                if (this.previewSourceKeyByTargetKey.TryGetValue(targetKey, out sourceKey))
                {
                    this.stagedKeys.Remove(sourceKey);
                }
            }

            this.selectedSourceKeys.Clear();
            this.selectedTargetPreviewKeys.Clear();
            this.RebuildStagedPreview();
            this.RenderSelections();
        }

        private void OnMoveAllLeft(object sender, EventArgs e)
        {
            if (this.IsExchange)
            {
                return;
            }

            this.ClearPreview();
        }

        private void OnMoveClick(object sender, EventArgs e)
        {
            if (!this.EnsureCarrierAction(CarrierEditPresenter.ActionMove))
            {
                return;
            }

            if (!this.ConfirmCommit(CarrierEditPresenter.ActionMove, this.stagedWafers.Rows.Count))
            {
                return;
            }

            DataTable wafers = this.stagedWafers.Copy();

            if (!this.CanExecuteCarrierAction(CarrierEditPresenter.ActionMove, wafers))
            {
                this.ShowToast(this.CarrierActionReason(CarrierEditPresenter.ActionMove), ToastKind.Warning);
                return;
            }

            this.stagedWafers = null;
            this.MoveBetween(this.SourceId(), this.TargetId(), wafers, "Nothing to move.");
        }

        private bool ConfirmCommit(string action, int count)
        {
            string verb = this.IsExchange && action == CarrierEditPresenter.ActionMove
                    ? "Exchange"
                    : action;

            return this.Confirm(
                    verb + " " + count.ToString("N0") + " wafers from "
                            + this.SourceId() + " to " + this.TargetId() + "?"
                            + Environment.NewLine
                            + "This commits the target carrier " + this.TargetId() + ".",
                    "Carrier Editor — " + action);
        }

        private void OnScrapClick(object sender, EventArgs e)
        {
            if (!this.EnsureCarrierAction(CarrierEditPresenter.ActionScrap))
            {
                return;
            }

            string sourceId = this.SourceId();
            string type = this.GetSelectedType();
            DataTable wafers = this.stagedWafers.Copy();

            if (!this.Confirm(
                    "Scrap " + wafers.Rows.Count.ToString("N0") + " wafers from " + sourceId + "?",
                    "Carrier Editor — Scrap"))
            {
                return;
            }

            if (!this.CanExecuteCarrierAction(CarrierEditPresenter.ActionScrap, wafers))
            {
                this.ShowToast(this.CarrierActionReason(CarrierEditPresenter.ActionScrap), ToastKind.Warning);
                return;
            }

            this.stagedWafers = null;

            this.RunAction(
                    delegate { return ScrapWafers(type, sourceId, wafers); },
                    delegate(DataActionResult reply)
                    {
                        this.ShowToast(
                                reply.Count.ToString("N0") + " wafers scrapped from "
                                        + sourceId + ".",
                                ToastKind.Success);
                        this.ReloadCarrierLists(sourceId, this.TargetId());
                    },
                    this.HandleCarrierActionRejected,
                    "Scrapping…");
        }

        private bool EnsureCarrierAction(string action)
        {
            if (this.CanExecuteCarrierAction(action))
            {
                return true;
            }

            this.ShowToast(this.CarrierActionReason(action), ToastKind.Warning);
            return false;
        }

        private string Description()
        {
            return (this.txtDescription.Text ?? string.Empty).Trim();
        }

        private string SourceId()
        {
            return this.cboSource.SelectedValue as string ?? string.Empty;
        }

        private string TargetId()
        {
            return this.cboTarget.SelectedValue as string ?? string.Empty;
        }

        private void MoveBetween(string fromId, string toId, DataTable wafers, string emptyMessage)
        {
            if (wafers.Rows.Count == 0)
            {
                this.ShowToast(emptyMessage, ToastKind.Warning);
                return;
            }

            if (fromId.Length == 0 || toId.Length == 0)
            {
                this.ShowToast("Select both carriers first.", ToastKind.Warning);
                return;
            }

            string type = this.GetSelectedType();

            if (this.IsExchange
                    && !string.Equals(type, ServerFields.Carrier.Foup, StringComparison.Ordinal))
            {
                this.ShowToast("Exchange moves a whole FOUP. Use Edit instead.", ToastKind.Warning);
                return;
            }

            string description = this.Description();

            this.RunAction(
                    delegate { return MoveWafers(type, fromId, toId, wafers, description); },
                    delegate(DataActionResult reply)
                    {
                        if (this.SourceLocked)
                        {
                            this.DurableChanged = true;
                        }

                        this.ShowToast(
                                reply.Count.ToString("N0") + " wafers moved to " + toId + ".",
                                ToastKind.Success);

                        if (this.IsExchange)
                        {
                            this.Close();
                            return;
                        }

                        this.ReloadCarrierLists(this.SourceId(), this.TargetId());
                    },
                    this.HandleCarrierActionRejected,
                    "Moving…");
        }

        private void HandleCarrierActionRejected(DataActionResult reply)
        {
            string sourceId = this.SourceId();
            string targetId = this.TargetId();

            this.mapSource.ClearSelection();
            this.mapTarget.ClearSelection();
            this.ClearPreview();

            if (reply != null && reply.CommunicationFailure)
            {
                this.DiscardCarrierState();
                this.ReloadCarrierLists(sourceId, targetId);
            }

            this.ShowToast(
                    reply == null || reply.Message.Length == 0
                            ? "Carrier action failed."
                            : reply.Message,
                    ToastKind.Warning);
        }



        private DataTable GetCarriers(string type)
        {
            DataTable received = this.RequestFields(
                    "GetDurableList", "DURABLE_TYPE", type ?? string.Empty).Table;

            return this.SourceLocked ? SelectSourceAndTargets(received) : received;
        }


        private DataActionResult MoveWafers(
                string type, string fromId, string toId, DataTable wafers, string description)
        {
            if (this.IsExchange)
            {
                return this.ExchangeDurableWafers(fromId, toId, wafers, description);
            }

            return this.MoveDurableWafers(type, fromId, toId, wafers, description);
        }

        private DataTable SelectSourceAndTargets(DataTable received)
        {
            DataTable selected = received == null ? new DataTable() : received.Clone();
            DataRow source = this.FindDurable(received, this.initialSourceId);

            if (source != null)
            {
                selected.ImportRow(source);
                DataTable targets = this.IsExchange
                        ? DurableManagementPresenter.ExchangeTargets(received, source)
                        : DurableManagementPresenter.EditTargets(received, source);

                foreach (DataRow target in targets.Rows)
                {
                    selected.ImportRow(target);
                }
            }

            return selected;
        }

        private DataTable GetDurableWafers(string durableId)
        {
            return this.RequestFields(
                    "GetDurableSlotList",
                    "DURABLE_ID", durableId ?? string.Empty,
                    "SUB_TYPE", string.Empty).Table;
        }

        private DataActionResult MoveDurableWafers(
                string type, string sourceId, string targetId, DataTable wafers, string description)
        {
            if (string.Equals(type, ServerFields.Carrier.Tray, StringComparison.Ordinal))
            {
                return this.RequestFields(
                        "EditDurable",
                        "SOURCE_ID", sourceId ?? string.Empty,
                        "TARGET_ID", targetId ?? string.Empty,
                        "CHIP_IDS", UnitIds(wafers, ServerFields.Lot.SubProdTyp.Chip),
                        "STUB_SLOT_NOS", SlotNos(wafers, ServerFields.Lot.SubProdTyp.Chip),
                        "LAMELLA_IDS", UnitIds(wafers, ServerFields.Lot.SubProdTyp.Lamella),
                        "LCC_SLOT_NOS", SlotNos(wafers, ServerFields.Lot.SubProdTyp.Lamella),
                        "DESCRIPTION", description);
            }

            return this.RequestFields(
                    "EditDurable",
                    "SOURCE_ID", sourceId ?? string.Empty,
                    "TARGET_ID", targetId ?? string.Empty,
                    "WAFER_IDS", UnitIds(wafers, ServerFields.Lot.SubProdTyp.Wafer),
                    "SLOT_NOS", SlotNos(wafers, ServerFields.Lot.SubProdTyp.Wafer),
                    "DESCRIPTION", description);
        }

        private DataActionResult ExchangeDurableWafers(
                string sourceId, string targetId, DataTable wafers, string description)
        {
            return this.RequestFields(
                    "ExchangeDurable",
                    "SOURCE_ID", sourceId ?? string.Empty,
                    "TARGET_ID", targetId ?? string.Empty,
                    "WAFER_IDS", UnitIds(wafers, ServerFields.Lot.SubProdTyp.Wafer),
                    "SLOT_NOS", SlotNos(wafers, ServerFields.Lot.SubProdTyp.Wafer),
                    "DESCRIPTION", description);
        }

        private static string UnitIds(DataTable wafers, string kind)
        {
            return Join(wafers, kind, ServerFields.Unit.WfId);
        }

        private static string SlotNos(DataTable wafers, string kind)
        {
            return Join(wafers, kind, ServerFields.Unit.SlotNo);
        }

        private static string Join(DataTable wafers, string kind, string column)
        {
            List<string> values = new List<string>();

            if (wafers != null)
            {
                foreach (DataRow row in wafers.Rows)
                {
                    string rowKind = TableHelper.CellText(
                            row, ServerFields.Lot.SubProdTyp.Column).Trim();

                    if (!string.Equals(rowKind, kind, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length == 0)
                    {
                        continue;
                    }

                    values.Add(TableHelper.CellText(row, column).Trim());
                }
            }

            return string.Join(",", values);
        }

        private DataRow FindDurable(DataTable durables, string durableId)
        {
            if (durables == null)
            {
                return null;
            }

            foreach (DataRow row in durables.Rows)
            {
                if (string.Equals(
                        TableHelper.CellText(row, ServerFields.Durable.DurableId).Trim(),
                        durableId,
                        StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }

        private DataActionResult ScrapWafers(string type, string carrierId, DataTable wafers)
        {
            if (string.Equals(type, ServerFields.Carrier.Tray, StringComparison.Ordinal))
            {
                return this.RequestFields(
                        "ScrapDurable",
                        "DURABLE_ID", carrierId ?? string.Empty,
                        "CHIP_IDS", UnitIds(wafers, ServerFields.Lot.SubProdTyp.Chip),
                        "STUB_SLOT_NOS", SlotNos(wafers, ServerFields.Lot.SubProdTyp.Chip),
                        "LAMELLA_IDS", UnitIds(wafers, ServerFields.Lot.SubProdTyp.Lamella),
                        "LCC_SLOT_NOS", SlotNos(wafers, ServerFields.Lot.SubProdTyp.Lamella));
            }

            return this.RequestFields(
                    "ScrapDurable",
                    "DURABLE_ID", carrierId ?? string.Empty,
                    "WAFER_IDS", UnitIds(wafers, ServerFields.Lot.SubProdTyp.Wafer),
                    "SLOT_NOS", SlotNos(wafers, ServerFields.Lot.SubProdTyp.Wafer));
        }

    }
}
