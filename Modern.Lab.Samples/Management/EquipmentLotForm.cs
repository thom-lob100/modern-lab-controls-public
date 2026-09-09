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
using Modern.Lab.Samples.Hosting;
using Modern.Lab.Samples.Hosting.ResponseContracts;
using Modern.Lab.Samples.Management.Contracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.Samples.Management.Services;
using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.WinForms.Controls.Display;

namespace Modern.Lab.Samples.Management
{
    public partial class EquipmentLotForm : ModernFormBase
    {
        private DataTable groupData;
        private static readonly ResponseContractSet Contracts = EquipmentLotContracts.Build();

        private DataTable equipmentData;
        private DataTable portData;
        private DataTable lotData;
        private const string comboLabelColumn = "LABEL";
        private const string comboValueColumn = "VALUE";
        private const string comboEnabledColumn = "CAN";
        private const string intervalSecondsColumn = "SECONDS";

        private DataTable requestData;
        private DataTable specimenData;
        private DataTable durableData;
        private RequestSnapshot requestSnapshot;

        private TableResponse equipmentCurrent;
        private TableResponse portCurrent;
        private TableResponse lotCurrent;
        private TableResponse requestCurrent;
        private TableResponse specimenCurrent;
        private TableResponse durableCurrent;

        private TableResponse equipmentReserved;
        private TableResponse portReserved;
        private TableResponse lotReserved;
        private TableResponse requestReserved;
        private TableResponse specimenReserved;
        private TableResponse durableReserved;

        private string decisionPortEqpId = string.Empty;

        private string durableTargetEqpId = string.Empty;
        private string durableTargetPortNm = string.Empty;

        private bool cyclePortsReflected;
        private bool cycleLotsReflected;
        private bool cycleRefresh;

        private const string channelGroups = "groups";
        private const string channelEquipments = "equipments";
        private const string channelPorts = "ports";
        private const string channelLots = "lots";
        private const string channelRequests = "requests";

        private const string channelDurables = "durables";

        private const string userId = "operator";

        private const double priorityWidth = 84d;
        private const double issueWidth = 84d;

        private const string lockGlyph = "\uE72E";
        private const string unlockGlyph = "\uE785";

        private readonly JobDecision decision = new JobDecision();
        private readonly System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer();

        private int refreshSerial;
        private int dependentSerial;

        private string portEqpId = string.Empty;
        private string requestLotId = string.Empty;
        private string requestSerialNo = string.Empty;

        private bool groupSetup;
        private bool gridBinding;
        private bool silentRefresh;

        private bool silentCycle;
        private int decisionHeight = -1;
        private int decisionSplitHeightSeen = -1;
        private int decisionSplitDistanceSeen = -1;
        private bool fittingColumns;
        private bool syncingListHeights;
        private int decisionPending;

        private int intervalSeconds;
        private int secondsLeft;

        public EquipmentLotForm()
        {
            this.InitializeComponent();

            this.InitializeModernForm(this.midPanel);

            this.DeferredResize = true;

            this.RegisterFindShortcut(this.gridLots, this.gridEqp);

            this.menuEqp.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
            this.menuPort.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
            this.menuLot.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
            this.menuLot.ShowItemToolTips = true;

            this.refreshTimer.Interval = 1000;
            this.refreshTimer.Tick += this.OnRefreshTick;
            this.Disposed += this.OnFormDisposed;

            this.splitRight.SizeChanged += this.OnBottomLayoutSizeChanged;
            this.splitDurableDecision.SizeChanged += this.OnBottomLayoutSizeChanged;
            this.actionCard.SizeChanged += this.OnActionColumnSizeChanged;
            this.lblRequestRemarkCaption.ForeColor = Modern.Lab.Theming.ModernTheme.TextSecondary;
            this.SyncActionColumns();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (this.decisionHeight < 0)
            {
                this.decisionHeight = this.splitDurableDecision.Panel2.Height;
                this.decisionSplitHeightSeen = this.splitDurableDecision.Height;
                this.decisionSplitDistanceSeen = this.splitDurableDecision.SplitterDistance;
            }

            this.FitDecisionPanel();
            this.SyncActionColumns();
        }


        private DataTable RequestGroups()
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetEqpGroupList");
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestEquipments(string group)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetEqpList");
            request.Append(" EQP_GRP=").Append(group);
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestPorts(string eqpId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetEqpPortList");
            request.Append(" EQP_ID=").Append(eqpId);
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestLots(string group, string eqpId)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetJobLotList");
            request.Append(" EQP_GRP=").Append(group);
            request.Append(" EQP_ID=").Append(eqpId);
            return this.Request(request.ToString()).Table;
        }

        private DataTable RequestDurables(string eqpId, string portNm)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetJobDurableList");
            request.Append(" EQP_ID=").Append(eqpId);
            request.Append(" PORT_NM=").Append(portNm);
            return this.Request(request.ToString()).Table;
        }


        private DataActionResult JobPrep(
                string eqpId, string inPort, string outPort, string lotId, string durableId, string description)
        {
            StringBuilder request = new StringBuilder();
            request.Append("JobPrep");
            request.Append(" EQP_ID=").Append(eqpId);
            request.Append(" IN_PORT=").Append(inPort);
            request.Append(" OUT_PORT=").Append(outPort);
            request.Append(" LOT_ID=").Append(lotId);
            request.Append(" DURABLE_ID=").Append(durableId);
            request.Append(" DESCRIPTION=").Append(description);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }

        private DataActionResult JobStart(string lotId, string description)
        {
            StringBuilder request = new StringBuilder();
            request.Append("JobStart");
            request.Append(" LOT_ID=").Append(lotId);
            request.Append(" DESCRIPTION=").Append(description);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }

        private DataActionResult JobEnd(string lotId, string description)
        {
            StringBuilder request = new StringBuilder();
            request.Append("JobEnd");
            request.Append(" LOT_ID=").Append(lotId);
            request.Append(" DESCRIPTION=").Append(description);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }

        private DataActionResult SetMode(string eqpId, string mode)
        {
            StringBuilder request = new StringBuilder();
            request.Append("SetEqpMode");
            request.Append(" EQP_ID=").Append(eqpId);
            request.Append(" MODE=").Append(mode);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }

        private DataActionResult SetAuto(string eqpId, bool on)
        {
            StringBuilder request = new StringBuilder();
            request.Append("SetEqpAuto");
            request.Append(" EQP_ID=").Append(eqpId);
            request.Append(" AUTO_YN=").Append(on ? ServerFields.Equipment.AutoYn.Y : ServerFields.Equipment.AutoYn.N);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }

        private DataActionResult SetPortStatus(string eqpId, string portNm, string action)
        {
            StringBuilder request = new StringBuilder();
            request.Append("SetEqpPortStatus");
            request.Append(" EQP_ID=").Append(eqpId);
            request.Append(" PORT_NM=").Append(portNm);
            request.Append(" ACTION=").Append(action);
            request.Append(" USER_ID=").Append(userId);
            return this.Request(request.ToString());
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            this.cboGroup.DisplayMember = ServerFields.Group.EqpGrpId;
            this.cboGroup.ValueMember = ServerFields.Group.EqpGrpId;

            this.cboInterval.DisplayMember = comboLabelColumn;
            this.cboInterval.ValueMember = intervalSecondsColumn;
            this.cboInterval.DataSource = EquipmentLotPresenter.IntervalOptions();
            this.cboInterval.SelectedValue = EquipmentLotPresenter.DefaultIntervalSeconds;
            this.ApplyInterval();

            this.decisionBar.Resize += this.OnDecisionBarResize;
            this.AlignLockButton();

            this.gridEqp.RowKeyMember = ServerFields.Equipment.EqpId;
            this.gridPorts.RowKeyMember = ServerFields.Port.PortNm;

            this.gridLots.RowKeyMember = ServerFields.Lot.LotId;
            this.gridLots.RowColorMember = EquipmentLotPresenter.JobColorColumn;
            this.gridLots.CellLinkClick += this.OnLotRequestLinkClick;

            this.gridDurables.RowKeyMember = ServerFields.Durable.DurableId;
            this.fieldRequest.FieldLinkClick += this.OnRequestFieldLinkClick;
            string[] tones = Modern.Lab.Theming.Palette.GetColors(4);
            this.kpiEquipment.Tone = tones[0];
            this.kpiPorts.Tone = tones[1];
            this.kpiLot.Tone = tones[2];
            this.kpiDurable.Tone = tones[3];

            this.badgeJob.SpinValues = EquipmentLotPresenter.JobStateStart;

            this.PopulateMenu(this.menuEqp, "Mode", EquipmentPortManagementPresenter.EquipmentActions, this.OnEquipmentMenuItemClick);
            this.PopulateMenu(this.menuPort, string.Empty, EquipmentPortManagementPresenter.PortActions, this.OnPortMenuItemClick);
            this.PopulateJobMenu();

            this.ddbPort.DisplayMember = comboLabelColumn;
            this.ddbPort.ValueMember = comboValueColumn;
            this.ddbPort.EnabledMember = comboEnabledColumn;
            this.ddbEquipment.DisplayMember = comboLabelColumn;
            this.ddbEquipment.ValueMember = comboValueColumn;
            this.ddbEquipment.EnabledMember = comboEnabledColumn;

            this.UpdateLockButton();
            this.RefreshDecisionPanel();
            this.RefreshActionStates();

            this.LoadGroups();
        }

        private void OnFormDisposed(object sender, EventArgs e)
        {
            this.refreshTimer.Stop();
            this.refreshTimer.Dispose();
        }

        private async void LoadGroups()
        {
            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelGroups, () => this.RequestGroups());

            if (!outcome.IsCurrent || outcome.Failure != null)
            {
                return;
            }

            this.groupData = outcome.Value;
            this.groupSetup = true;

            try
            {
                this.cboGroup.DataSource = outcome.Value;

                if (outcome.Value != null && outcome.Value.Rows.Count > 0)
                {
                    this.cboGroup.SelectedIndex = 0;
                }
            }
            finally
            {
                this.groupSetup = false;
            }

            this.ExecuteSearch();
        }

        private void OnGroupChanged(object sender, EventArgs e)
        {
            if (this.groupSetup)
            {
                return;
            }

            this.ExecuteSearch();
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            this.secondsLeft = this.intervalSeconds;
            this.UpdateCountdown();
            this.ExecuteSearch();
        }

        private void OnRefreshTick(object sender, EventArgs e)
        {
            if (this.intervalSeconds <= 0)
            {
                return;
            }

            this.secondsLeft--;

            if (this.secondsLeft > 0)
            {
                this.UpdateCountdown();
                return;
            }

            if (this.QueryInProgress || this.ActionInProgress)
            {
                this.secondsLeft = 1;
                this.UpdateCountdown();
                return;
            }

            this.secondsLeft = this.intervalSeconds;
            this.UpdateCountdown();
            this.ExecuteSearch(true);
        }

        private void OnIntervalChanged(object sender, EventArgs e)
        {
            this.ApplyInterval();
        }

        private void ApplyInterval()
        {
            this.intervalSeconds = EquipmentLotPresenter.IntervalSeconds(this.cboInterval.SelectedValue);
            this.secondsLeft = this.intervalSeconds;
            this.refreshTimer.Enabled = this.intervalSeconds > 0;
            this.UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            string text = EquipmentLotPresenter.CountdownText(this.secondsLeft, this.intervalSeconds);
            this.badgeCountdown.Text = text.Length == 0 ? "-" : text;
            this.badgeCountdown.Visible = text.Length > 0;
        }

        private void OnDecisionSplitterMoved(object sender, SplitterEventArgs e)
        {
            Modern.Lab.WinForms.Controls.Layout.ModernSplitContainer lower = this.splitDurableDecision;
            bool dragged = lower.Height == this.decisionSplitHeightSeen && lower.SplitterDistance != this.decisionSplitDistanceSeen;

            if (this.decisionHeight >= 0 && !this.fittingColumns && dragged)
            {
                this.decisionHeight = lower.Panel2.Height;
                this.FitDecisionPanel();
            }

            this.decisionSplitHeightSeen = lower.Height;
            this.decisionSplitDistanceSeen = lower.SplitterDistance;
        }

        private void OnListSplitterMoved(object sender, SplitterEventArgs e)
        {
            if (this.syncingListHeights)
            {
                return;
            }

            Modern.Lab.WinForms.Controls.Layout.ModernSplitContainer source =
                    sender as Modern.Lab.WinForms.Controls.Layout.ModernSplitContainer;
            Modern.Lab.WinForms.Controls.Layout.ModernSplitContainer target =
                    object.ReferenceEquals(source, this.splitLeft) ? this.splitLotRequest : this.splitLeft;

            if (source == null)
            {
                return;
            }

            int room = target.Height - target.SplitterWidth;

            if (room < target.Panel1MinSize + target.Panel2MinSize)
            {
                return;
            }

            int distance = Math.Max(target.Panel1MinSize, Math.Min(source.SplitterDistance, room - target.Panel2MinSize));
            this.syncingListHeights = true;

            try
            {
                target.SplitterDistance = distance;
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                this.syncingListHeights = false;
            }
        }

        private void OnBottomLayoutSizeChanged(object sender, EventArgs e)
        {
            this.FitDecisionPanel();
            this.SyncActionColumns();
        }

        private void OnActionColumnSizeChanged(object sender, EventArgs e)
        {
            this.SyncActionColumns();
        }

        private void SyncActionColumns()
        {
            this.lblTarget.Width = Math.Max(0, this.ddbEquipment.Left - this.lblTarget.Left - this.actionCard.Padding.Left);
        }

        private void FitDecisionPanel()
        {
            if (this.decisionHeight < 0 || this.fittingColumns)
            {
                return;
            }

            Modern.Lab.WinForms.Controls.Layout.ModernSplitContainer lower = this.splitDurableDecision;
            int room = lower.Height - lower.SplitterWidth;

            if (room < lower.Panel1MinSize + lower.Panel2MinSize)
            {
                return;
            }

            int decision = Math.Max(lower.Panel2MinSize, Math.Min(this.decisionHeight, room - lower.Panel1MinSize));
            int upper = room - decision;

            this.fittingColumns = true;

            try
            {
                if (lower.SplitterDistance != upper)
                {
                    lower.SplitterDistance = upper;
                }
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                this.fittingColumns = false;
            }
        }

        private void OnDecisionBarResize(object sender, EventArgs e)
        {
            this.AlignLockButton();
        }

        private void AlignLockButton()
        {
            int left = this.decisionBar.ClientSize.Width - this.btnLock.Width - 4;

            if (left < 0)
            {
                left = 0;
            }

            if (this.btnLock.Left != left)
            {
                this.btnLock.Left = left;
            }
        }

        private void OnLockClick(object sender, EventArgs e)
        {
            this.decision.Locked = !this.decision.Locked;
            this.UpdateLockButton();
            this.RefreshDecisionPanel();
            this.RefreshActionStates();
        }

        private void UpdateLockButton()
        {
            this.btnLock.IconGlyph = this.decision.Locked ? lockGlyph : unlockGlyph;
            this.btnLock.Text = this.decision.Locked ? "Locked" : "Lock";
            this.decisionCard.TitleRightText = this.decision.Locked ? "Locked" : "Top priority";
        }

        private string SelectedGroupId()
        {
            return (Convert.ToString(this.cboGroup.SelectedValue) ?? string.Empty).Trim();
        }

        private DataRow SelectedGroupRow()
        {
            return EquipmentLotPresenter.FindById(this.groupData, ServerFields.Group.EqpGrpId, this.SelectedGroupId());
        }

        private string SelectedEquipmentId()
        {
            DataRowView equipment = this.gridEqp.SelectedItem as DataRowView;
            return equipment == null ? string.Empty : TableHelper.CellText(equipment.Row, ServerFields.Equipment.EqpId).Trim();
        }

        private string SelectedLotId()
        {
            DataRowView lot = this.gridLots.SelectedItem as DataRowView;
            return lot == null ? string.Empty : TableHelper.CellText(lot.Row, ServerFields.Lot.LotId).Trim();
        }

        private string SelectedRequestSerialNo()
        {
            DataRowView lot = this.gridLots.SelectedItem as DataRowView;
            return lot == null ? string.Empty : TableHelper.CellText(lot.Row, ServerFields.Lot.ReqSerialNo).Trim();
        }

        private async void ExecuteSearch(bool silent = false)
        {
            string group = this.SelectedGroupId();
            this.silentCycle = silent;

            if (group.Length == 0)
            {
                return;
            }

            string keepEqpId = this.decision.EqpId;

            IDisposable busy = silent ? null : this.Busy("Loading equipment...", this.cboGroup.Text);

            if (!silent)
            {
                this.BindEquipments(null, keepEqpId, false);
            }

            LoadOutcome<DataTable> outcome;

            try
            {
                outcome = await this.FetchAsync(
                        channelEquipments, () => this.RequestEquipments(group), silent);
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

            this.BindEquipments(outcome.Failure != null ? null : outcome.Value, keepEqpId, silent);
        }

        private void BindEquipments(DataTable equipments, string keepEqpId, bool silent)
        {
            bool merged;
            this.equipmentData = this.BindJudged(
                    EquipmentLotContracts.EquipmentTable, this.equipmentData, ref this.equipmentCurrent, ref this.equipmentReserved,
                    equipments, ServerFields.Equipment.EqpId, silent, out merged);

            if (Judged(this.equipmentCurrent))
            {
                EquipmentLotPresenter.MarkAutoCan(this.equipmentData);
            }

            ConfigureGrid(this.gridEqp, this.equipmentData, Judged(this.equipmentCurrent), columns => AutoVocabulary(columns
                    .BadgeColor(ServerFields.Priority, EquipmentLotPresenter.PriorityColorColumn)
                    .Badge(ServerFields.Equipment.CommStatTyp.Column, ServerFields.Equipment.MesEqpStatCd.Column)
                    .BadgeWidth(ServerFields.Equipment.CommStatTyp.Column, ServerFields.Equipment.CommStatTyp.All)
                    .BadgeWidth(ServerFields.Equipment.MesEqpStatCd.Column, ServerFields.Equipment.MesEqpStatCd.All)
                    .Spin(ServerFields.Equipment.MesEqpStatCd.Column, ServerFields.Equipment.MesEqpStatCd.Run)
                    .YesNo(ServerFields.Equipment.AutoYn.Column, EquipmentLotPresenter.AutoCanColumn)));

            this.decision.DropDetached();
            DataTable table = this.equipmentData;
            DataRow equipment = this.decision.Locked
                    ? EquipmentLotPresenter.FindById(table, ServerFields.Equipment.EqpId, keepEqpId)
                    : EquipmentLotPresenter.TopPriority(table);

            this.gridBinding = true;

            try
            {
                if (!merged)
                {
                    this.gridEqp.DataSource = table;
                    this.SelectRow(this.gridEqp, table, ServerFields.Equipment.EqpId, TableHelper.CellText(equipment, ServerFields.Equipment.EqpId).Trim());
                }
                else if (this.gridEqp.SelectedItem == null)
                {
                    this.SelectRow(this.gridEqp, table, ServerFields.Equipment.EqpId, TableHelper.CellText(equipment, ServerFields.Equipment.EqpId).Trim());
                }
            }
            finally
            {
                this.gridBinding = false;
            }

            this.eqpCard.TitleRightText = table == null
                    ? string.Empty
                    : table.Rows.Count.ToString("N0") + " equipment";
            this.RefreshContractNotice();

            string previousEqpId = this.decision.EqpId;
            this.decision.Equipment = equipment;
            this.refreshSerial++;

            this.silentRefresh = merged && this.decision.EqpId == previousEqpId && this.decision.EqpId.Length > 0;

            this.LoadPorts(this.SelectedEquipmentId(), this.silentRefresh);
            this.ApplyEquipmentDecision();
        }

        private static void FollowServerOrder(DataTable table, DataTable source, string keyColumn)
        {
            if (table == null)
            {
                return;
            }

            EquipmentLotPresenter.MarkServerOrder(table, source, keyColumn);
            string sort = EquipmentLotPresenter.ServerOrderColumn + " ASC";

            if (table.DefaultView.Sort != sort)
            {
                table.DefaultView.Sort = sort;
            }
        }

        private void SelectRow(Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid, DataTable table, string column, string id)
        {
            if (table == null || id.Length == 0)
            {
                return;
            }

            DataView view = table.DefaultView;

            for (int index = 0; index < view.Count; index++)
            {
                if (TableHelper.CellText(view[index].Row, column).Trim() == id)
                {
                    if (grid.SelectedIndex != index)
                    {
                        grid.SelectedIndex = index;
                    }

                    return;
                }
            }
        }

        private void ApplyEquipmentDecision()
        {
            string eqpId = this.decision.EqpId;

            if (!this.silentRefresh)
            {
                this.ClearDecisionDependents();
            }

            this.ResolveDecisionPorts();
            this.RefreshDecisionPanel();
            this.RefreshActionStates();

            this.cycleRefresh = this.silentRefresh;
            this.cyclePortsReflected = this.portEqpId != eqpId;
            this.cycleLotsReflected = false;

            if (eqpId.Length > 0)
            {
                this.LoadDecisionLots(this.SelectedGroupId(), eqpId);
                return;
            }

            this.SyncDecisionDurables(false);
        }

        private void ClearDecisionDependents()
        {
            this.InvalidateChannel(channelLots);
            this.InvalidateChannel(channelDurables);

            this.lotData = null;
            this.lotCurrent = null;
            this.lotReserved = null;
            this.gridLots.DataSource = null;
            this.durableData = null;
            this.durableCurrent = null;
            this.durableReserved = null;
            this.gridDurables.DataSource = null;
            this.durableTargetEqpId = string.Empty;
            this.durableTargetPortNm = string.Empty;
            this.durableCard.Text = EquipmentLotPresenter.DurableListTitle(string.Empty);
            this.ClearRequests();
        }

        private void ClearRequests()
        {
            this.InvalidateChannel(channelRequests);
            this.requestLotId = string.Empty;
            this.requestSerialNo = string.Empty;
            this.requestSnapshot = null;
            this.requestData = null;
            this.requestCurrent = null;
            this.requestReserved = null;
            this.specimenData = null;
            this.specimenCurrent = null;
            this.specimenReserved = null;
            this.BindRequestFields(null, string.Empty);
            this.gridSpecimens.DataSource = null;
            this.UpdateRequestTitle();
            this.RefreshContractNotice();
        }

        private void LoadDecisionLots(string group, string eqpId)
        {
            bool silent = this.silentRefresh;
            this.LoadDependent(channelLots, () => this.RequestLots(group, eqpId), table => this.BindLots(table, silent), !silent, silent);
        }

        private void SyncDecisionDurables(bool refresh)
        {
            string eqpId = this.decision.EqpId;
            bool portsKnown = eqpId.Length > 0 && this.decisionPortEqpId == eqpId;
            string portNm = EquipmentLotPresenter.JobOutPortNm(this.decision.Lot);

            if (portNm.Length == 0 && portsKnown)
            {
                portNm = this.decision.OutPortNm;
            }

            if (portNm.Length == 0 && !portsKnown && eqpId == this.durableTargetEqpId)
            {
                return;
            }

            if (eqpId == this.durableTargetEqpId && portNm == this.durableTargetPortNm)
            {
                if (refresh && portNm.Length > 0)
                {
                    this.LoadDependent(channelDurables, () => this.RequestDurables(eqpId, portNm), table => this.BindDurables(table, true), false, true);
                }

                return;
            }

            this.InvalidateChannel(channelDurables);
            this.durableData = null;
            this.gridDurables.DataSource = null;
            this.durableTargetEqpId = eqpId;
            this.durableTargetPortNm = portNm;
            this.durableCard.Text = EquipmentLotPresenter.DurableListTitle(string.Empty);

            if (eqpId.Length == 0 || portNm.Length == 0)
            {
                this.decision.Durable = null;
                this.dependentSerial++;
                this.RefreshDecisionPanel();
                this.RefreshActionStates();
                return;
            }

            this.LoadDependent(channelDurables, () => this.RequestDurables(eqpId, portNm), table => this.BindDurables(table, false), true, false);
        }

        private void SyncDecisionDurablesIfReady()
        {
            if (this.cyclePortsReflected && this.cycleLotsReflected)
            {
                this.SyncDecisionDurables(this.cycleRefresh);
                this.cycleRefresh = false;
            }
        }

        private string TargetDurableType()
        {
            DataRow port = this.portEqpId == this.durableTargetEqpId
                    ? EquipmentLotPresenter.FindById(this.portData, ServerFields.Port.PortNm, this.durableTargetPortNm)
                    : null;
            string type = EquipmentLotPresenter.PortDurableType(port);

            if (type.Length == 0 && this.durableData != null && this.durableData.Rows.Count > 0)
            {
                type = TableHelper.CellText(this.durableData.Rows[0], ServerFields.Durable.DurableType).Trim();
            }

            return type;
        }

        private async void LoadDependent(string channel, Func<DataTable> request, Action<DataTable> bind, bool busy, bool silent)
        {
            if (busy)
            {
                this.decisionPending++;
                this.decisionBusy.Busy = true;
            }

            LoadOutcome<DataTable> outcome;

            try
            {
                outcome = await this.FetchAsync(channel, request, silent);
            }
            finally
            {
                if (busy)
                {
                    this.decisionPending--;

                    if (this.decisionPending == 0 && !this.decisionBusy.IsDisposed)
                    {
                        this.decisionBusy.Busy = false;
                    }
                }
            }

            if (!outcome.IsCurrent)
            {
                return;
            }

            bind(outcome.Failure != null ? null : outcome.Value);
        }

        private DataTable BindJudged(
                string tableId, DataTable bound, ref TableResponse current, ref TableResponse reserved, DataTable incoming,
                string keyColumn, bool silent, out bool merged)
        {
            merged = false;
            reserved = null;

            if (incoming == null)
            {
                current = null;
                return null;
            }

            TableReception reception = TableJudgment.Receive(
                    tableId, incoming, Contracts, EquipmentLotPresenter.ScreenColumns);

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

            DataTable normalized = reception.Normalized;
            merged = silent && bound != null && current != null
                    && current.State != TableResponseState.MissingRequired
                    && TableJudgment.SameSchema(current.Table, normalized);
            DataTable table = merged ? bound : normalized;

            if (merged)
            {
                TableMerge.Apply(table, normalized, keyColumn, EquipmentLotPresenter.ScreenColumns);
            }

            FollowServerOrder(table, merged ? normalized : null, keyColumn);

            current = TableJudgment.Judge(reception, table, Contracts);
            EquipmentLotPresenter.MarkPriorityColors(table);
            return table;
        }

        private static bool Judged(TableResponse current)
        {
            return current != null && current.State != TableResponseState.MissingRequired;
        }

        private DataRow ValidEquipment(DataRow row)
        {
            return Judged(this.equipmentCurrent) && row != null && !TableJudgment.IsInvalid(row) ? row : null;
        }

        private DataRow ValidPort(DataRow row)
        {
            return Judged(this.portCurrent) && row != null && !TableJudgment.IsInvalid(row) ? row : null;
        }

        private static void ConfigureGrid(
                Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid, DataTable table, bool judged,
                Func<GridColumns, GridColumns> configure)
        {
            if (table == null)
            {
                return;
            }

            if (!judged)
            {
                GridColumns.Of(table).Apply(grid);
                return;
            }

            GridColumns columns = configure(GridColumns.Of(table))
                    .Hide(EquipmentLotPresenter.ServerOrderColumn, EquipmentLotPresenter.AutoCanColumn);

            if (TableJudgment.HasInvalidRows(table))
            {
                columns.First(ServerFields.Priority, TableJudgment.IssueColumn);

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
            else
            {
                columns.Hide(TableJudgment.IssueColumn);
            }

            columns.Apply(grid);
        }

        private static GridColumns AutoVocabulary(GridColumns columns)
        {
            foreach (ModernDataGridColumn column in columns.ToArray())
            {
                if (column.DataPropertyName == ServerFields.Equipment.AutoYn.Column)
                {
                    column.CheckTrueValue = ServerFields.Equipment.AutoYn.Y;
                    column.CheckFalseValue = ServerFields.Equipment.AutoYn.N;
                }
            }

            return columns;
        }

        private void RefreshContractNotice()
        {
            string text = EquipmentLotPresenter.BannerText(
                    new TableResponse[]
                    {
                        this.equipmentCurrent, this.portCurrent, this.lotCurrent, this.durableCurrent, this.requestCurrent,
                        this.specimenCurrent
                    },
                    new TableResponse[]
                    {
                        this.equipmentReserved, this.portReserved, this.lotReserved, this.durableReserved, this.requestReserved,
                        this.specimenReserved
                    },
                    Contracts);

            this.SetContractNotice(text);
        }

        private static bool InvalidSelection(Modern.Lab.WinForms.Controls.Data.ModernDataGrid grid)
        {
            DataRowView view = grid.SelectedItem as DataRowView;
            return view != null && TableJudgment.IsInvalid(view.Row);
        }

        private void OnEqpSelectionChanged(object sender, EventArgs e)
        {
            if (this.gridBinding)
            {
                return;
            }

            this.LoadPorts(this.SelectedEquipmentId(), false);
            this.RefreshActionStates();
        }

        private void LoadPorts(string eqpId, bool silent)
        {
            bool invalid = InvalidSelection(this.gridEqp);
            bool same = silent && !invalid && eqpId == this.portEqpId && this.portData != null;

            if (!same)
            {
                this.InvalidateChannel(channelPorts);
                this.portData = null;
                this.portCurrent = null;
                this.portReserved = null;
                this.gridPorts.DataSource = null;
                this.RefreshContractNotice();
            }

            this.portEqpId = eqpId;
            this.portCard.Text = eqpId.Length > 0 ? "Port List — " + eqpId : "Port List";

            if (eqpId.Length > 0 && !invalid)
            {
                this.LoadDependent(channelPorts, () => this.RequestPorts(eqpId), table => this.BindPorts(table, same), !same && eqpId == this.decision.EqpId, same);
            }
        }

        private void BindPorts(DataTable ports, bool silent)
        {
            bool merged;
            this.portData = this.BindJudged(
                    EquipmentLotContracts.PortTable, this.portData, ref this.portCurrent, ref this.portReserved, ports, ServerFields.Port.PortNm, silent, out merged);

            ConfigureGrid(this.gridPorts, this.portData, Judged(this.portCurrent), columns => columns
                    .BadgeColor(ServerFields.Priority, EquipmentLotPresenter.PriorityColorColumn)
                    .Badge(ServerFields.Port.PortTyp.Column, ServerFields.Port.TransferStatCd.Column)
                    .BadgeWidth(ServerFields.Port.PortTyp.Column, ServerFields.Port.PortTyp.All)
                    .BadgeWidth(ServerFields.Port.TransferStatCd.Column, ServerFields.Port.TransferStatCd.All)
                    .Spin(ServerFields.Port.TransferStatCd.Column, ServerFields.Port.TransferStatCd.Processing));

            if (!merged)
            {
                this.gridPorts.DataSource = this.portData;
            }

            this.RefreshContractNotice();
            this.decision.DropDetached();
            this.ResolveDecisionPorts();

            if (this.portEqpId == this.decision.EqpId)
            {
                this.cyclePortsReflected = true;
                this.SyncDecisionDurablesIfReady();
            }

            this.dependentSerial++;
            this.RefreshDecisionPanel();
            this.RefreshActionStates();
        }

        private void ResolveDecisionPorts()
        {
            if (this.portData == null || this.portEqpId.Length == 0 || this.portEqpId != this.decision.EqpId)
            {
                return;
            }

            this.decisionPortEqpId = this.portEqpId;

            if (this.decision.Locked)
            {
                this.decision.InPort = this.decision.InPortNm.Length > 0
                        ? EquipmentLotPresenter.FindById(this.portData, ServerFields.Port.PortNm, this.decision.InPortNm)
                        : EquipmentLotPresenter.TopPort(this.portData, ServerFields.Port.PortTyp.Input);
                this.decision.OutPort = this.decision.OutPortNm.Length > 0
                        ? EquipmentLotPresenter.FindById(this.portData, ServerFields.Port.PortNm, this.decision.OutPortNm)
                        : EquipmentLotPresenter.TopPort(this.portData, ServerFields.Port.PortTyp.Output);

                if (this.decision.OutPort == null && this.decision.InPort != null
                        && EquipmentLotPresenter.PortType(this.decision.InPort) == ServerFields.Port.PortTyp.InputOutput)
                {
                    this.decision.OutPort = this.decision.InPort;
                }

                return;
            }

            DataRow inPort = EquipmentLotPresenter.TopPort(this.portData, ServerFields.Port.PortTyp.Input);
            DataRow outPort = EquipmentLotPresenter.TopPort(this.portData, ServerFields.Port.PortTyp.Output);

            if (inPort != null && EquipmentLotPresenter.PortType(inPort) == ServerFields.Port.PortTyp.InputOutput)
            {
                outPort = inPort;
            }

            this.decision.InPort = inPort;
            this.decision.OutPort = outPort;
        }

        private void BindLots(DataTable lots, bool silent)
        {
            string previousLotId = this.decision.LotId;
            string colorsBefore = EquipmentLotPresenter.JobColorSignature(this.lotData);
            bool merged;
            this.lotData = this.BindJudged(
                    EquipmentLotContracts.LotTable, this.lotData, ref this.lotCurrent, ref this.lotReserved, lots, ServerFields.Lot.LotId, silent, out merged);
            this.decision.DropDetached();

            if (Judged(this.lotCurrent))
            {
                EquipmentLotPresenter.MarkJobColors(this.lotData);
            }

            ConfigureGrid(this.gridLots, this.lotData, Judged(this.lotCurrent), columns => columns
                    .BadgeColor(ServerFields.Priority, EquipmentLotPresenter.PriorityColorColumn)
                    .BadgeSpin(ServerFields.Priority, EquipmentLotPresenter.PrioritySpinColumn)
                    .Badge(ServerFields.Lot.LastEventCd.Column, ServerFields.Lot.MesProcStatCd.Column)
                    .BadgeWidth(ServerFields.Lot.MesProcStatCd.Column, ServerFields.Lot.MesProcStatCd.All)
                    .Spin(ServerFields.Lot.LastEventCd.Column, EquipmentLotPresenter.JobStateStart)
                    .Link(ServerFields.Lot.ReqSerialNo));

            if (!merged || (this.lotData != null && colorsBefore != EquipmentLotPresenter.JobColorSignature(this.lotData)))
            {
                this.gridLots.DataSource = this.lotData;
            }

            this.RefreshContractNotice();

            this.lotCard.TitleRightText = this.lotData == null ? string.Empty : this.lotData.Rows.Count.ToString("N0") + " lots";

            this.decision.Lot = this.decision.Locked
                    ? EquipmentLotPresenter.FindById(this.lotData, ServerFields.Lot.LotId, previousLotId)
                    : EquipmentLotPresenter.TopPriority(this.lotData);

            this.cycleLotsReflected = true;
            this.SyncDecisionDurablesIfReady();

            this.gridBinding = true;

            try
            {
                if (!silent || this.gridLots.SelectedItem == null)
                {
                    this.SelectRow(this.gridLots, this.lotData, ServerFields.Lot.LotId, this.decision.LotId);
                }
            }
            finally
            {
                this.gridBinding = false;
            }

            this.dependentSerial++;
            this.LoadRequests(this.SelectedLotId(), this.SelectedRequestSerialNo(), silent);
            this.RefreshDecisionPanel();
            this.RefreshActionStates();
        }

        private void BindDurables(DataTable durables, bool silent)
        {
            string keepDurable = this.decision.DurableId;
            bool merged;
            this.durableData = this.BindJudged(
                    EquipmentLotContracts.DurableTable, this.durableData, ref this.durableCurrent, ref this.durableReserved, durables, ServerFields.Durable.DurableId, silent, out merged);

            ConfigureGrid(this.gridDurables, this.durableData, Judged(this.durableCurrent), columns => columns
                    .BadgeColor(ServerFields.Priority, EquipmentLotPresenter.PriorityColorColumn)
                    .Badge(ServerFields.Durable.Mode, ServerFields.Durable.WfLoadStatCd.Column));

            if (!merged)
            {
                this.gridDurables.DataSource = this.durableData;
            }

            this.RefreshContractNotice();
            this.decision.DropDetached();
            this.durableCard.Text = EquipmentLotPresenter.DurableListTitle(this.TargetDurableType());

            this.decision.Durable = this.decision.Locked
                    ? EquipmentLotPresenter.FindById(this.durableData, ServerFields.Durable.DurableId, keepDurable)
                    : EquipmentLotPresenter.TopPriority(this.durableData);

            this.dependentSerial++;
            this.RefreshDecisionPanel();
            this.RefreshActionStates();
        }

        private void OnLotSelectionChanged(object sender, EventArgs e)
        {
            if (this.gridBinding)
            {
                return;
            }

            this.LoadRequests(this.SelectedLotId(), this.SelectedRequestSerialNo(), false);
        }

        private void OnPortSelectionChanged(object sender, EventArgs e)
        {
            if (this.gridBinding)
            {
                return;
            }

            this.RefreshActionStates();
        }

        private async void LoadRequests(string lotId, string requestSerialNo, bool silent)
        {
            string key = (requestSerialNo ?? string.Empty).Trim();
            this.ClearRequests();

            this.requestLotId = lotId;
            this.requestSerialNo = key;
            this.UpdateRequestTitle();

            this.lblRequestEmpty.Text = lotId.Length == 0 ? "Select a lot" : "No request on this lot";

            if (key.Length == 0)
            {
                return;
            }

            LoadOutcome<RequestSnapshot> outcome = await this.FetchAsync(
                    channelRequests, () => this.FetchRequestSnapshot(key), silent);

            if (!outcome.IsCurrent || key != this.requestSerialNo)
            {
                return;
            }

            if (outcome.Failure != null)
            {
                this.BindRequests(null, false);
                return;
            }

            this.BindRequests(outcome.Value, silent);
        }

        private RequestSnapshot FetchRequestSnapshot(string requestSerialNo)
        {
            System.Threading.Tasks.Task<DataTable> masterTask = System.Threading.Tasks.Task.Run(
                    () => this.RequestTable("GetReqList", requestSerialNo));
            System.Threading.Tasks.Task<DataTable> detailTask = System.Threading.Tasks.Task.Run(
                    () => this.RequestTable("GetRequestInfo", requestSerialNo));
            System.Threading.Tasks.Task.WaitAll(masterTask, detailTask);
            return RequestSnapshot.Create(requestSerialNo, masterTask.Result, detailTask.Result);
        }

        private DataTable RequestTable(string operation, string requestSerialNo)
        {
            StringBuilder request = new StringBuilder(operation);
            request.Append(' ').Append(ServerFields.Request.ReqSerialNo).Append('=').Append(requestSerialNo);
            return this.Request(request.ToString()).Table;
        }

        private void BindRequests(RequestSnapshot snapshot, bool silent)
        {
            DataTable master = snapshot == null ? null : snapshot.Master.Copy();
            DataTable details = snapshot == null ? null : snapshot.Details.Copy();

            bool merged;
            this.requestData = this.BindJudged(
                    EquipmentLotContracts.RequestTable, this.requestData, ref this.requestCurrent, ref this.requestReserved,
                    master, ServerFields.Request.ReqSerialNo, silent, out merged);

            this.BindRequestFields(this.requestData, snapshot == null ? string.Empty : snapshot.Remarks);

            bool specimensMerged;
            this.specimenData = this.BindJudged(
                    EquipmentLotContracts.SpecimenTable, this.specimenData, ref this.specimenCurrent, ref this.specimenReserved,
                    details, null, false, out specimensMerged);

            ConfigureGrid(this.gridSpecimens, this.specimenData, Judged(this.specimenCurrent), columns => columns);

            if (!specimensMerged)
            {
                this.gridSpecimens.DataSource = this.specimenData;
            }

            this.requestSnapshot = snapshot;
            this.RefreshContractNotice();
        }

        private void BindRequestFields(DataTable overview, string remarks)
        {
            GridColumns columns = GridColumns.Of(overview);
            if (Judged(this.requestCurrent))
            {
                columns.Hide(EquipmentLotPresenter.ServerOrderColumn, EquipmentLotPresenter.AutoCanColumn);
                if (!TableJudgment.HasInvalidRows(overview))
                {
                    columns.Hide(TableJudgment.IssueColumn);
                }
            }

            ModernDataGridColumn[] members = columns.ToArray();
            DataRow row = overview == null || overview.Rows.Count == 0 ? null : overview.Rows[0];
            List<ModernFieldDefinition> fields = new List<ModernFieldDefinition>();

            for (int index = 0; index < members.Length; index++)
            {
                string member = members[index].DataPropertyName;
                fields.Add(new ModernFieldDefinition(member)
                {
                    IsLink = Judged(this.requestCurrent)
                            && string.Equals(member, ReqSerialNoColumn, StringComparison.OrdinalIgnoreCase)
                });
            }

            this.fieldRequest.DefineFields(fields.ToArray());
            this.fieldRequest.SetRow(row);
            int fieldRows = (fields.Count + this.fieldRequest.Columns - 1) / this.fieldRequest.Columns;
            int fieldHeight = Math.Max(1, fieldRows) * 40 * this.DeviceDpi / 96;
            int remarkHeight = row == null ? 0 : 72 * this.DeviceDpi / 96;
            this.tableRequestMaster.RowStyles[0].Height = fieldHeight;
            this.tableRequestMaster.RowStyles[1].Height = remarkHeight;
            this.tableRequestMaster.Height = fieldHeight + remarkHeight;
            this.FitRequestHeader(this.tableRequestMaster.Height);
            this.lblRequestRemarkCaption.Text = "Remarks";
            this.lblRequestRemark.Text = row == null ? string.Empty : ValueOrDash(remarks);
            this.panelRequestRemark.Visible = row != null;
            this.tableRequestMaster.Visible = row != null;
            this.lblRequestEmpty.Visible = row == null;
        }

        private void FitRequestHeader(int desiredHeight)
        {
            int room = this.splitRequest.Height - this.splitRequest.SplitterWidth;

            if (room < this.splitRequest.Panel1MinSize + this.splitRequest.Panel2MinSize)
            {
                return;
            }

            int maximum = room - this.splitRequest.Panel2MinSize;
            int height = Math.Max(this.splitRequest.Panel1MinSize, Math.Min(desiredHeight, maximum));

            if (this.splitRequest.SplitterDistance == height)
            {
                return;
            }

            try
            {
                this.splitRequest.SplitterDistance = height;
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void OnRequestFieldLinkClick(object sender, ModernFieldLinkClickEventArgs e)
        {
            if (string.Equals(e.Member, ReqSerialNoColumn, StringComparison.OrdinalIgnoreCase))
            {
                this.ShowRequestSnapshot(e.Value);
            }
        }

        private void OnLotRequestLinkClick(object sender, GridButtonClickEventArgs e)
        {
            DataRowView row = e.Item as DataRowView;

            if (row == null || !string.Equals(e.DataPropertyName, ReqSerialNoColumn, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            this.ShowRequestSnapshot(TableHelper.CellText(row.Row, e.DataPropertyName));
        }

        private void ShowRequestSnapshot(string requestSerialNo)
        {
            string key = (requestSerialNo ?? string.Empty).Trim();
            RequestSnapshot snapshot = this.requestSnapshot;

            if (snapshot == null || key.Length == 0 || key != this.requestSerialNo || key != snapshot.RequestSerialNo)
            {
                return;
            }

            using (RequestInfoDialogForm dialog = new RequestInfoDialogForm())
            {
                dialog.SetRequest(
                        snapshot.RequestSerialNo, snapshot.Master.Copy(), snapshot.Details.Copy(), snapshot.Remarks);
                dialog.ShowDialog(this);
            }
        }

        private void UpdateRequestTitle()
        {
            this.requestCard.Text = this.requestSerialNo.Length > 0 ? "Request List — " + this.requestSerialNo : "Request List";
        }

        private static DataRow Candidate(object selectedItem)
        {
            DataRowView view = selectedItem as DataRowView;
            return view == null ? null : CandidateRow(view.Row);
        }

        private static DataRow CandidateRow(DataRow row)
        {
            if (!JobDecision.IsLive(row) || !EquipmentLotPresenter.IsCandidate(row))
            {
                return null;
            }

            return row;
        }

        private void OnEqpRowDoubleClick(object sender, EventArgs e)
        {
            DataRow equipment = Candidate(this.gridEqp.SelectedItem);

            if (equipment == null
                    || TableHelper.CellText(equipment, ServerFields.Equipment.EqpId).Trim() == this.decision.EqpId)
            {
                return;
            }

            this.decision.Equipment = equipment;
            this.decision.InPort = null;
            this.decision.OutPort = null;
            this.silentRefresh = false;
            this.ApplyEquipmentDecision();
        }

        private void OnPortRowDoubleClick(object sender, EventArgs e)
        {
            DataRow port = Candidate(this.gridPorts.SelectedItem);

            if (port == null)
            {
                return;
            }

            if (this.portEqpId != this.decision.EqpId)
            {
                DataRow equipment = CandidateRow(
                        EquipmentLotPresenter.FindById(this.equipmentData, ServerFields.Equipment.EqpId, this.portEqpId));

                if (equipment == null)
                {
                    return;
                }

                this.decision.Equipment = equipment;
                this.decision.InPort = null;
                this.decision.OutPort = null;
                this.ApplyPortChoice(port);
                this.silentRefresh = false;
                this.ClearDecisionDependents();
                this.RefreshDecisionPanel();
                this.RefreshActionStates();
                this.cycleRefresh = false;
                this.cyclePortsReflected = true;
                this.cycleLotsReflected = false;
                this.LoadDecisionLots(this.SelectedGroupId(), this.decision.EqpId);
                this.SyncDecisionDurables(false);
                return;
            }

            this.ApplyPortChoice(port);
            this.SyncDecisionDurables(false);
            this.RefreshDecisionPanel();
            this.RefreshActionStates();
        }

        private void ApplyPortChoice(DataRow port)
        {
            this.decisionPortEqpId = this.portEqpId;
            string type = EquipmentLotPresenter.PortType(port);

            if (type == ServerFields.Port.PortTyp.Input || type == ServerFields.Port.PortTyp.InputOutput)
            {
                this.decision.InPort = port;
            }

            if (type == ServerFields.Port.PortTyp.Output || type == ServerFields.Port.PortTyp.InputOutput)
            {
                this.decision.OutPort = port;
            }
        }

        private void OnLotRowDoubleClick(object sender, EventArgs e)
        {
            DataRow lot = Candidate(this.gridLots.SelectedItem);

            if (lot == null)
            {
                return;
            }

            this.decision.Lot = lot;
            this.SyncDecisionDurables(false);
            this.RefreshDecisionPanel();
            this.RefreshActionStates();
        }

        private void OnDurableRowDoubleClick(object sender, EventArgs e)
        {
            DataRow durable = Candidate(this.gridDurables.SelectedItem);

            if (durable == null)
            {
                return;
            }

            this.decision.Durable = durable;
            this.RefreshDecisionPanel();
            this.RefreshActionStates();
        }

        private void RefreshDecisionPanel()
        {
            DataRow lot = this.decision.Lot;
            DataRow summary = EquipmentLotPresenter.JobSummary(this.decision);

            string inPort = TableHelper.CellText(summary, EquipmentLotPresenter.SummaryInPort);
            string outPort = TableHelper.CellText(summary, EquipmentLotPresenter.SummaryOutPort);
            string durableType = TableHelper.CellText(summary, ServerFields.Durable.DurableType);
            string mode = TableHelper.CellText(summary, ServerFields.Equipment.CommStatTyp.Column);
            string jobState = TableHelper.CellText(summary, ServerFields.Lot.LastEventCd.Column);

            this.kpiEquipment.Value = ValueOrDash(TableHelper.CellText(summary, ServerFields.Equipment.EqpId));
            this.kpiPorts.Value = inPort.Length == 0 && outPort.Length == 0
                    ? "-"
                    : ValueOrDash(inPort) + " → " + ValueOrDash(outPort);
            this.kpiLot.Value = ValueOrDash(this.decision.LotId);
            string lotFlowOper = EquipmentLotPresenter.LotFlowOper(lot);
            this.kpiLot.Title = lotFlowOper.Length == 0 ? "Lot" : "Lot  " + lotFlowOper;
            string sourceDurable = TableHelper.CellText(summary, ServerFields.Durable.DurableId);
            string goalDurable = TableHelper.CellText(summary, EquipmentLotPresenter.SummaryGoalDurable);
            this.kpiDurable.Title = (durableType.Length == 0 ? "Durable" : "Durable · " + durableType)
                    + (sourceDurable.Length == 0 ? string.Empty : "  from " + sourceDurable);
            this.kpiDurable.Value = ValueOrDash(goalDurable);

            this.badgeMode.Text = ValueOrDash(mode);
            this.badgeMode.ColorValue = mode;
            this.badgeJob.Text = jobState.Length == 0 ? "No job" : jobState;
            this.badgeJob.ColorValue = jobState;
            this.badgeJob.SpinValues = ServerFields.Lot.LastEventCd.JobStart;
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        private void RefreshActionStates()
        {
            DataRowView equipment = this.gridEqp.SelectedItem as DataRowView;
            DataRow equipmentRow = this.ValidEquipment(equipment == null ? null : equipment.Row);

            DataRowView port = this.gridPorts.SelectedItem as DataRowView;
            DataRow portRow = this.ValidPort(port == null ? null : port.Row);

            this.ddbEquipment.DataSource = EquipmentPortManagementPresenter.BuildMenuTable(
                    EquipmentPortManagementPresenter.EquipmentActions, equipmentRow, null);
            this.ddbEquipment.Enabled = equipmentRow != null;

            this.ddbPort.DataSource = EquipmentPortManagementPresenter.BuildMenuTable(
                    EquipmentPortManagementPresenter.PortActions, equipmentRow, portRow);
            this.ddbPort.Enabled = portRow != null;

            ActionGate gate = this.BuildGate();

            this.btnJobPrep.Enabled = this.Allows(gate, EquipmentLotPresenter.ActionJobPrep);
            this.btnJobStart.Enabled = this.Allows(gate, EquipmentLotPresenter.ActionJobStart);
            this.btnJobEnd.Enabled = this.Allows(gate, EquipmentLotPresenter.ActionJobEnd);
            this.lblTarget.Text = EquipmentLotPresenter.StatusLineText(this.decision, gate);
        }

        private ActionGate BuildGate()
        {
            ActionGate gate = new ActionGate(Contracts);

            Observe(gate, EquipmentLotContracts.EquipmentTable, this.equipmentCurrent, this.equipmentData, this.decision.Equipment);

            Observe(
                    gate, EquipmentLotContracts.PortTable, this.portCurrent, this.portData,
                    SelectionSlot.Of(EquipmentLotContracts.PortInSlot, this.decision.InPort),
                    SelectionSlot.Of(EquipmentLotContracts.PortOutSlot, this.decision.OutPort));

            Observe(gate, EquipmentLotContracts.LotTable, this.lotCurrent, this.lotData, this.decision.Lot);
            Observe(gate, EquipmentLotContracts.DurableTable, this.durableCurrent, this.durableData, this.decision.Durable);
            Observe(gate, EquipmentLotContracts.RequestTable, this.requestCurrent, this.requestData);
            Observe(gate, EquipmentLotContracts.SpecimenTable, this.specimenCurrent, this.specimenData);

            return gate;
        }

        private static void Observe(
                ActionGate gate, string tableId, TableResponse current, DataTable bound, DataRow selected)
        {
            Observe(gate, tableId, current, bound, SelectionSlot.Of(ActionContract.DefaultSlot, selected));
        }

        private static void Observe(
                ActionGate gate, string tableId, TableResponse current, DataTable bound, params SelectionSlot[] slots)
        {
            if (current == null || bound == null)
            {
                return;
            }

            SelectionSlot[] mapped = new SelectionSlot[slots.Length];

            for (int index = 0; index < slots.Length; index++)
            {
                mapped[index] = SelectionSlot.Of(slots[index].Name, RowIn(current, bound, slots[index].Row));
            }

            gate.Observe(tableId, current, mapped);
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

        private bool Allows(ActionGate gate, string key)
        {
            return gate.CanExecute(key) && EquipmentLotPresenter.CanExecute(key, this.decision);
        }

        private void OnJobPrepClick(object sender, EventArgs e)
        {
            this.ExecuteJob(EquipmentLotPresenter.ActionJobPrep);
        }

        private void OnJobStartClick(object sender, EventArgs e)
        {
            this.ExecuteJob(EquipmentLotPresenter.ActionJobStart);
        }

        private void OnJobEndClick(object sender, EventArgs e)
        {
            this.ExecuteJob(EquipmentLotPresenter.ActionJobEnd);
        }

        private void OnEquipmentActionClicked(object sender, DropDownItemClickedEventArgs e)
        {
            this.ExecuteEquipmentAction(e.Value as string);
        }

        private void ExecuteJob(string key)
        {
            if (!this.Allows(this.BuildGate(), key))
            {
                return;
            }

            EquipmentJobDialogOptions options = new EquipmentJobDialogOptions();
            options.Title = EquipmentLotPresenter.JobLabel(key) + " — " + this.decision.LotId;
            options.SourceCaption = options.Title;
            options.OkText = EquipmentLotPresenter.JobLabel(key);
            options.Source = EquipmentLotPresenter.JobSummary(this.decision);
            options.SourceFields = EquipmentLotPresenter.JobSummaryFields();

            this.OpenJobDialog(key, options);
        }

        protected virtual void OpenJobDialog(string key, EquipmentJobDialogOptions options)
        {
            using (EquipmentJobDialogForm dialog = new EquipmentJobDialogForm(options))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.ProcessJob(key, dialog.Description);
                }
            }
        }

        private void ProcessJob(string key, string description)
        {
            string lotId = this.decision.LotId;
            string eqpId = this.decision.EqpId;
            string inPort = this.decision.InPortNm;
            string outPort = this.decision.OutPortNm;
            string durableId = this.decision.DurableId;

            switch (key)
            {
                case EquipmentLotPresenter.ActionJobPrep:
                    this.Process(
                            () => this.JobPrep(eqpId, inPort, outPort, lotId, durableId, description),
                            "Preparing job for " + lotId + "…",
                            () => this.ExecuteSearch(true));
                    break;

                case EquipmentLotPresenter.ActionJobStart:
                    this.Process(
                            () => this.JobStart(lotId, description),
                            "Starting job " + lotId + "…",
                            () => this.ExecuteSearch(true));
                    break;

                case EquipmentLotPresenter.ActionJobEnd:
                    this.Process(
                            () => this.JobEnd(lotId, description),
                            "Ending job " + lotId + "…",
                            () => this.ExecuteSearch(true));
                    break;
            }
        }

        private void ExecuteEquipmentAction(string key)
        {
            DataRowView selected = this.gridEqp.SelectedItem as DataRowView;
            DataRow equipment = this.ValidEquipment(selected == null ? null : selected.Row);

            if (equipment == null
                    || !EquipmentPortManagementPresenter.CanExecute(key, equipment, null))
            {
                return;
            }

            string eqpId = TableHelper.CellText(equipment, ServerFields.Equipment.EqpId).Trim();

            if (key == EquipmentPortManagementPresenter.ActionAuto)
            {
                this.ExecuteAutoToggle(equipment, !EquipmentPortManagementPresenter.IsAuto(equipment));
                return;
            }

            if (!this.Confirm(
                    "Change " + eqpId + " from " + EquipmentPortManagementPresenter.Mode(equipment) + " to "
                            + Modern.Lab.WinForms.Controls.Dialogs.ModernMessageDialog.Emphasis(key) + "?",
                    "Confirm"))
            {
                return;
            }

            this.Process(
                    () => this.SetMode(eqpId, key),
                    "Changing mode to " + key + "…",
                    () => this.ExecuteSearch(true));
        }

        private void ExecuteAutoToggle(DataRow equipment, bool turnOn)
        {
            if (this.ValidEquipment(equipment) == null
                    || !EquipmentPortManagementPresenter.CanExecute(EquipmentPortManagementPresenter.ActionAuto, equipment, null))
            {
                return;
            }

            string eqpId = TableHelper.CellText(equipment, ServerFields.Equipment.EqpId).Trim();

            if (!this.Confirm(
                    "Turn Auto " + Modern.Lab.WinForms.Controls.Dialogs.ModernMessageDialog.Emphasis(turnOn ? "on" : "off")
                            + " for " + eqpId + "?",
                    "Confirm"))
            {
                return;
            }

            this.Process(
                    () => this.SetAuto(eqpId, turnOn),
                    turnOn ? "Turning Auto on…" : "Turning Auto off…",
                    () => this.ExecuteSearch(true));
        }

        private void OnEqpCellCheckChanged(object sender, GridCheckChangedEventArgs e)
        {
            DataRowView equipment = e.Item as DataRowView;

            if (equipment == null || e.DataPropertyName != ServerFields.Equipment.AutoYn.Column
                    || equipment.Row.RowState == DataRowState.Detached || equipment.Row.RowState == DataRowState.Deleted)
            {
                return;
            }

            equipment.Row[ServerFields.Equipment.AutoYn.Column] = e.IsChecked ? ServerFields.Equipment.AutoYn.N : ServerFields.Equipment.AutoYn.Y;
            this.ExecuteAutoToggle(equipment.Row, e.IsChecked);
        }

        private void ExecutePortAction(string key)
        {
            DataRow equipment = this.ValidEquipment(
                    EquipmentLotPresenter.FindById(this.equipmentData, ServerFields.Equipment.EqpId, this.portEqpId));
            DataRowView selected = this.gridPorts.SelectedItem as DataRowView;
            DataRow port = this.ValidPort(selected == null ? null : selected.Row);

            if (equipment == null || port == null
                    || !EquipmentPortManagementPresenter.CanExecute(key, equipment, port))
            {
                return;
            }

            string eqpId = this.portEqpId;
            string portNm = TableHelper.CellText(port, ServerFields.Port.PortNm).Trim();

            if (!this.Confirm(
                    "Change " + eqpId + " port " + portNm
                            + " from " + EquipmentPortManagementPresenter.PortStatus(port) + " to "
                            + Modern.Lab.WinForms.Controls.Dialogs.ModernMessageDialog.Emphasis(
                                    EquipmentPortManagementPresenter.PortStatusAfter(key))
                            + " (" + key + ")?",
                    "Confirm"))
            {
                return;
            }

            this.Process(
                    () => this.SetPortStatus(eqpId, portNm, key),
                    key + " port " + portNm + "…",
                    () => this.ExecuteSearch(true));
        }

        private void Process(Func<DataActionResult> call, string busyText, Action refresh)
        {
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
                IList<EquipmentPortAction> actions, EventHandler onClick)
        {
            menu.Items.Clear();

            if (!string.IsNullOrEmpty(headerText))
            {
                menu.Items.Add(new ToolStripLabel(headerText));
            }

            foreach (EquipmentPortAction action in actions)
            {
                if (action.SeparatorBefore)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }

                ToolStripMenuItem item = new ToolStripMenuItem(action.Label);
                item.Tag = action.Key;
                item.Click += onClick;
                menu.Items.Add(item);
            }
        }

        private void PopulateJobMenu()
        {
            this.menuLot.Items.Clear();
            this.menuLot.Items.Add(new ToolStripLabel("Execute"));

            foreach (LotAction action in EquipmentLotPresenter.JobActions)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(action.Label);
                item.Tag = action.Key;
                item.Click += this.OnJobMenuItemClick;
                this.menuLot.Items.Add(item);
            }
        }

        private void OnMenuEqpOpening(object sender, CancelEventArgs e)
        {
            DataRowView selected = this.gridEqp.SelectedItem as DataRowView;
            DataRow equipment = this.ValidEquipment(selected == null ? null : selected.Row);

            e.Cancel = !ApplyMenuStates(
                    this.menuEqp, EquipmentPortManagementPresenter.EquipmentActions, equipment, null, equipment != null);
        }

        private void OnMenuPortOpening(object sender, CancelEventArgs e)
        {
            DataRow equipment = this.ValidEquipment(
                    EquipmentLotPresenter.FindById(this.equipmentData, ServerFields.Equipment.EqpId, this.portEqpId));
            DataRowView selected = this.gridPorts.SelectedItem as DataRowView;
            DataRow port = this.ValidPort(selected == null ? null : selected.Row);

            e.Cancel = !ApplyMenuStates(
                    this.menuPort, EquipmentPortManagementPresenter.PortActions, equipment, port, equipment != null && port != null);
        }

        private void OnMenuLotOpening(object sender, CancelEventArgs e)
        {
            DataRow lot = Candidate(this.gridLots.SelectedItem);

            if (lot != null && !object.ReferenceEquals(lot, this.decision.Lot))
            {
                this.decision.Lot = lot;
                this.RefreshDecisionPanel();
                this.RefreshActionStates();
            }

            ActionGate jobGate = this.BuildGate();

            foreach (object entry in this.menuLot.Items)
            {
                ToolStripMenuItem item = entry as ToolStripMenuItem;

                if (item != null && EquipmentLotPresenter.IsJobKey(item.Tag as string))
                {
                    item.Enabled = this.Allows(jobGate, item.Tag as string);
                    item.ToolTipText = EquipmentLotPresenter.ActionReasonText(jobGate, item.Tag as string, this.decision);
                }
            }

            e.Cancel = this.decision.Equipment == null;
        }

        private static bool ApplyMenuStates(
                ContextMenuStrip menu, IList<EquipmentPortAction> actions,
                DataRow equipment, DataRow port, bool hasTarget)
        {
            if (!hasTarget)
            {
                return false;
            }

            foreach (object entry in menu.Items)
            {
                ToolStripMenuItem item = entry as ToolStripMenuItem;

                if (item == null)
                {
                    continue;
                }

                string key = item.Tag as string;

                foreach (EquipmentPortAction action in actions)
                {
                    if (action.Key == key)
                    {
                        item.Enabled = EquipmentPortManagementPresenter.CanExecute(key, equipment, port);
                        item.Checked = EquipmentPortManagementPresenter.IsChecked(key, equipment);
                        item.Text = EquipmentPortManagementPresenter.LabelOf(action, equipment, false);
                        break;
                    }
                }
            }

            return true;
        }

        private void OnEquipmentMenuItemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item != null)
            {
                this.ExecuteEquipmentAction(item.Tag as string);
            }
        }

        private void OnPortActionClicked(object sender, DropDownItemClickedEventArgs e)
        {
            this.ExecutePortAction(e.Value as string);
        }

        private void OnPortMenuItemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item != null)
            {
                this.ExecutePortAction(item.Tag as string);
            }
        }

        private void OnJobMenuItemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item != null)
            {
                this.ExecuteJob(item.Tag as string);
            }
        }

        private sealed class RequestSnapshot
        {
            private RequestSnapshot(string requestSerialNo, DataTable master, DataTable details, string remarks)
            {
                this.RequestSerialNo = requestSerialNo;
                this.Master = master;
                this.Details = details;
                this.Remarks = remarks;
            }

            public string RequestSerialNo { get; private set; }

            public DataTable Master { get; private set; }

            public DataTable Details { get; private set; }

            public string Remarks { get; private set; }

            public static RequestSnapshot Create(string requestSerialNo, DataTable master, DataTable details)
            {
                DataTable masterCopy = master == null ? new DataTable("REQUEST") : master.Copy();
                DataTable detailCopy = details == null ? new DataTable("REQUEST_DETAIL") : details.Copy();
                string remarks = string.Empty;

                if (masterCopy.Columns.Contains(ServerFields.Request.Details))
                {
                    if (masterCopy.Rows.Count > 0)
                    {
                        remarks = TableHelper.CellText(masterCopy.Rows[0], ServerFields.Request.Details);
                    }

                    masterCopy.Columns.Remove(ServerFields.Request.Details);
                }

                return new RequestSnapshot(requestSerialNo, masterCopy, detailCopy, remarks);
            }
        }
    }
}
