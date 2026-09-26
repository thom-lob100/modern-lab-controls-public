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
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.Samples.Contracts;
using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.WinForms.Controls.Display;

using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.NewItems;

namespace Modern.Lab.Samples
{

    public partial class EquipmentPortManagementForm : ModernFormBase
    {
        private static readonly ResponseContractSet Contracts = EquipmentPortContracts.Build();

        private DataTable equipmentData;
        private readonly NewItemTracker newItems = new NewItemTracker();
        private readonly NewItemTracker eqpHistoryNewItems = new NewItemTracker();
        private readonly NewItemTracker portHistoryNewItems = new NewItemTracker();
        private string newItemCriteria;
        private DataTable portData;
        private DataTable eqpHistoryData;
        private DataTable portHistoryData;

        private DataTable equipmentView;

        private const string comboLabelColumn = "LABEL";
        private const string comboValueColumn = "VALUE";
        private const string comboEnabledColumn = "CAN";

        private const string treeKeyBase = "TREE_KEY";
        private const string treeParentBase = "TREE_PARENT";
        private string treeKeyColumn = treeKeyBase;
        private string treeParentColumn = treeParentBase;

        private TableResponse equipmentCurrent;
        private TableResponse portCurrent;

        private TableResponse equipmentReserved;
        private TableResponse portReserved;

        private const string channelGroups = "groups";
        private const string channelEquipments = "equipments";
        private const string channelPorts = "ports";
        private const string channelEqpHistory = "eqpHistory";
        private const string channelPortHistory = "portHistory";
        private const int portSelectionDelay = 300;
        private const string eqpHistoryTitle = "Eqp History";
        private const string portHistoryTitle = "Port History";

        private const double issueWidth = 84d;

        private bool groupSetup;
        private bool treeBinding;

        private readonly DependentCover portCover;
        private readonly DependentCover historyCover;

        public EquipmentPortManagementForm()
        {
            this.InitializeComponent();

            this.portCover = new DependentCover(this.portBusy);
            this.historyCover = new DependentCover(this.historyBusy);

            this.InitializeModernForm(this.midPanel);

            this.DeferredResize = true;

            this.RegisterFindShortcut(this.gridEqpHistory, this.gridPortHistory);

            this.menuEqp.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
            this.menuPort.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            this.cboGroup.DisplayMember = ServerFields.Group.EqpGrpId;
            this.cboGroup.ValueMember = ServerFields.Group.EqpGrpId;

            this.treeEqp.IdMember = treeKeyColumn;
            this.treeEqp.ParentIdMember = treeParentColumn;
            this.treeEqp.DisplayMember = ServerFields.Equipment.EqpId;
            this.gridPorts.RowKeyMember = ServerFields.Port.PortNm;
            this.gridEqpHistory.RowKeyMember = ServerFields.Equipment.EqpId + "," + ServerFields.Item.TimeKey;
            this.gridPortHistory.RowKeyMember = ServerFields.Port.PortNm + "," + ServerFields.Item.TimeKey;

            this.PopulateMenu(this.menuEqp, "Mode", EquipmentPortManagementPresenter.EquipmentActions, this.OnEquipmentMenuItemClick);
            this.PopulateMenu(this.menuPort, string.Empty, EquipmentPortManagementPresenter.PortActions, this.OnPortMenuItemClick);

            this.ddbEquipment.DisplayMember = comboLabelColumn;
            this.ddbEquipment.ValueMember = comboValueColumn;
            this.ddbEquipment.EnabledMember = comboEnabledColumn;
            this.ddbPort.DisplayMember = comboLabelColumn;
            this.ddbPort.ValueMember = comboValueColumn;
            this.ddbPort.EnabledMember = comboEnabledColumn;
            this.RefreshActionStates();

            this.LoadGroups();
        }

        private async void LoadGroups()
        {
            LoadOutcome<DataTable> outcome = await this.FetchAsync(
                    channelGroups, () => this.RequestGroups());

            if (!outcome.IsCurrent || outcome.Failure != null)
            {
                return;
            }

            this.groupSetup = true;

            try
            {
                this.cboGroup.DataSource = outcome.Value;
                this.cboGroup.CheckAll();
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
            this.ExecuteSearch();
        }

        private string SelectedEqpGrpIds()
        {
            return EquipmentPortManagementPresenter.GroupFilter(this.cboGroup.CheckedValues);
        }

        private string SelectedEquipmentId()
        {
            return TableHelper.CellText(this.SelectedEquipmentRow(), ServerFields.Equipment.EqpId).Trim();
        }

        private DataRowView SelectedEquipmentView()
        {
            DataRowView selected = this.treeEqp.SelectedItem as System.Data.DataRowView;

            if (selected == null || this.equipmentView == null || this.equipmentData == null)
            {
                return null;
            }

            int index = this.equipmentView.Rows.IndexOf(selected.Row);

            if (index < 0 || index >= this.equipmentData.Rows.Count)
            {
                return null;
            }

            return this.equipmentData.DefaultView[index];
        }

        private DataRow SelectedEquipmentRow()
        {
            DataRowView view = this.SelectedEquipmentView();
            return view == null ? null : view.Row;
        }

        private async void ExecuteSearch(bool silent = false)
        {
            string eqpGrpIds = this.SelectedEqpGrpIds();
            this.newItems.BeginQuery();
            this.RefreshActionStates();
            string keepEquipmentId = this.SelectedEquipmentId();

            IDisposable busy = silent ? null : this.Busy("Loading equipment...", this.cboGroup.Text);

            LoadOutcome<DataTable> outcome;

            try
            {
                outcome = await this.FetchAsync(
                        channelEquipments, () => this.RequestEquipments(eqpGrpIds), silent);
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

            this.newItemCriteria = NewItemTracker.Criteria(NewItemTracker.SelectionSet(eqpGrpIds));
            this.BindEquipments(outcome.Value, keepEquipmentId, silent);
        }

        private void BindEquipments(DataTable equipments, string keepEquipmentId, bool silent)
        {
            this.equipmentData = this.BindJudged(
                    EquipmentPortContracts.EquipmentTable, ref this.equipmentCurrent, ref this.equipmentReserved, equipments, silent);
            bool judged = Judged(this.equipmentCurrent);
            this.equipmentData = this.newItems.Accept(this.equipmentData, ServerFields.Equipment.EqpId, this.newItemCriteria, judged, true);

            bool flat = !judged || TableJudgment.HasInvalidRows(this.equipmentData);
            this.treeKeyColumn = UniqueColumn(this.equipmentData, treeKeyBase);
            this.treeParentColumn = UniqueColumn(this.equipmentData, treeParentBase);
            this.treeEqp.IdMember = this.treeKeyColumn;
            this.treeEqp.ParentIdMember = this.treeParentColumn;
            this.equipmentView = EquipmentTreeView(this.equipmentData, flat, this.treeKeyColumn, this.treeParentColumn);

            GridColumns treeColumns = EquipmentTreeColumns(this.equipmentView);
            if (this.newItems.HasNewItems)
            {
                treeColumns.First(NewItemTracker.ColumnName).Badge(NewItemTracker.ColumnName);
            }
            else if (judged)
            {
                treeColumns.Hide(NewItemTracker.ColumnName);
            }
            TableJudgment.IssueBadge(treeColumns, flat && judged);
            treeColumns.Apply(this.treeEqp);

            EquipmentFields(judged ? this.equipmentCurrent.Table : this.equipmentData)
                    .Apply(this.fieldEqpInfo);

            string selectId = EquipmentPortManagementPresenter.SelectionAfterBind(this.equipmentData, keepEquipmentId);

            this.treeBinding = true;

            try
            {
                this.treeEqp.DataSource = this.equipmentView;
                this.treeEqp.SelectedValue = this.TreeKeyOf(selectId);
            }
            finally
            {
                this.treeBinding = false;
            }

            this.eqpCard.TitleRightText = this.equipmentData == null
                    ? string.Empty
                    : this.equipmentData.Rows.Count.ToString("N0") + " units";

            this.ApplyEquipmentSelection(silent);
        }

        private static DataTable EquipmentTreeView(DataTable source, bool flat, string keyColumn, string parentColumn)
        {
            if (source == null)
            {
                return null;
            }

            DataTable view = source.Copy();
            view.Columns.Add(keyColumn, typeof(string));
            view.Columns.Add(parentColumn, typeof(string));

            for (int index = 0; index < view.Rows.Count; index++)
            {
                DataRow row = view.Rows[index];
                row[keyColumn] = flat
                        ? index.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : TableHelper.CellText(row, ServerFields.Equipment.EqpId).Trim();
                row[parentColumn] = flat ? string.Empty : TableHelper.CellText(row, ServerFields.Equipment.ParentEqpId).Trim();
            }

            return view;
        }

        private static string UniqueColumn(DataTable table, string baseName)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (table != null)
            {
                foreach (DataColumn column in table.Columns)
                {
                    names.Add(column.ColumnName);
                }
            }

            if (!names.Contains(baseName))
            {
                return baseName;
            }

            for (int suffix = 1; ; suffix++)
            {
                string candidate = baseName + "_" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if (!names.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        private object TreeKeyOf(string eqpId)
        {
            if (string.IsNullOrEmpty(eqpId) || this.equipmentData == null || this.equipmentView == null)
            {
                return null;
            }

            for (int index = 0; index < this.equipmentData.Rows.Count; index++)
            {
                DataRow row = this.equipmentData.Rows[index];

                if (!TableJudgment.IsInvalid(row) && TableHelper.CellText(row, ServerFields.Equipment.EqpId).Trim() == eqpId)
                {
                    return this.equipmentView.Rows[index][treeKeyColumn];
                }
            }

            return null;
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
                    tableId, incoming, Contracts, EquipmentPortManagementPresenter.ScreenColumns);

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

        private void OnEquipmentSelectionChanged(object sender, EventArgs e)
        {
            if (this.treeBinding)
            {
                return;
            }

            this.ApplyEquipmentSelection(false);
        }

        private void ApplyEquipmentSelection(bool silent)
        {
            DataRowView equipment = this.SelectedEquipmentView();

            if (equipment == null)
            {
                this.fieldEqpInfo.ClearValues();
                this.portCard.Text = "Port List";
                this.ClearDependents();
                this.RefreshActionStates();
                return;
            }

            string eqpId = TableHelper.CellText(equipment.Row, ServerFields.Equipment.EqpId).Trim();

            this.fieldEqpInfo.SetRow(equipment.Row);
            this.fieldEqpInfo.SetValue(ServerFields.Equipment.AutoYn.Column, EquipmentPortManagementPresenter.AutoText(equipment.Row));
            this.portCard.Text = "Port List — " + eqpId;

            bool loadable = Judged(this.equipmentCurrent) && !TableJudgment.IsInvalid(equipment.Row);

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

            this.pageEqpHistory.Text = eqpHistoryTitle + " — " + eqpId;
            this.LoadDependents(eqpId, silent);
        }

        private void InvalidateDependents()
        {
            this.InvalidateChannel(channelPorts);
            this.InvalidateChannel(channelEqpHistory);
            this.InvalidateChannel(channelPortHistory);
        }

        private void ClearDependents()
        {
            this.portCover.Reset();
            this.historyCover.Reset();
            this.InvalidateChannel(channelPorts);
            this.InvalidateChannel(channelEqpHistory);
            this.InvalidateChannel(channelPortHistory);

            this.portData = null;
            this.portCurrent = null;
            this.portReserved = null;
            this.gridPorts.DataSource = null;
            this.eqpHistoryData = null;
            this.gridEqpHistory.DataSource = null;
            this.portHistoryData = null;
            this.gridPortHistory.DataSource = null;
            this.pageEqpHistory.Text = eqpHistoryTitle;
            this.pagePortHistory.Text = portHistoryTitle;
        }

        private void LoadDependents(string eqpId, bool silent)
        {
            this.LoadDependent(channelPorts, () => this.RequestPorts(eqpId), table => this.BindPorts(table, silent), silent, this.portCover);
            this.LoadDependent(channelEqpHistory, () => this.RequestEqpHistory(eqpId),
                    table => this.BindEqpHistory(table, NewItemTracker.Criteria(eqpId)), silent, this.historyCover);
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

        private void BindPorts(DataTable ports, bool silent)
        {
            this.portData = this.BindJudged(
                    EquipmentPortContracts.PortTable, ref this.portCurrent, ref this.portReserved, ports, silent);

            if (!Judged(this.portCurrent))
            {
                GridColumns.Of(this.portData == null ? null : this.portData.Copy()).Bind(this.gridPorts);
            }
            else
            {
                GridColumns portColumns = PortColumns(this.portData);
                TableJudgment.IssueBadge(portColumns, TableJudgment.HasInvalidRows(this.portData));
                portColumns.Bind(this.gridPorts);
            }

            this.RefreshActionStates();
            this.ApplyPortSelection(silent);
        }

        private void BindEqpHistory(DataTable history, string criteria)
        {
            this.eqpHistoryData = this.eqpHistoryNewItems.Accept(history, this.gridEqpHistory.RowKeyMember, criteria);

            EqpHistoryColumns(this.eqpHistoryData).Bind(this.gridEqpHistory);
            NewItemTint.Bind(this.gridEqpHistory, this.eqpHistoryNewItems, this.gridEqpHistory.RowKeyMember, null, false);
        }

        private void BindPortHistory(DataTable history, string criteria)
        {
            this.portHistoryData = this.portHistoryNewItems.Accept(history, this.gridPortHistory.RowKeyMember, criteria);

            PortHistoryColumns(this.portHistoryData).Bind(this.gridPortHistory);
            NewItemTint.Bind(this.gridPortHistory, this.portHistoryNewItems, this.gridPortHistory.RowKeyMember, null, false);
        }

        private void OnPortSelectionChanged(object sender, EventArgs e)
        {
            this.RefreshActionStates();
            this.Debounce(channelPortHistory, portSelectionDelay, new MethodInvoker(delegate { this.ApplyPortSelection(false); }));
        }

        private void ApplyPortSelection(bool silent)
        {
            this.CancelDebounce(channelPortHistory);

            DataRowView equipment = this.SelectedEquipmentView();
            DataRowView port = this.gridPorts.SelectedItem as DataRowView;
            string portNm = port == null ? string.Empty : TableHelper.CellText(port.Row, ServerFields.Port.PortNm).Trim();

            if (equipment == null || portNm.Length == 0 || TableJudgment.IsInvalid(port.Row))
            {
                this.InvalidateChannel(channelPortHistory);
                this.portHistoryData = null;
                this.gridPortHistory.DataSource = null;
                this.pagePortHistory.Text = portHistoryTitle;
                return;
            }

            string eqpId = TableHelper.CellText(equipment.Row, ServerFields.Equipment.EqpId).Trim();

            this.pagePortHistory.Text = portHistoryTitle + " — " + portNm;
            this.LoadDependent(
                    channelPortHistory,
                    () => this.RequestPortHistory(eqpId, portNm),
                    table => this.BindPortHistory(table, NewItemTracker.Criteria(eqpId, portNm)), silent, this.historyCover);
        }

        private void RefreshActionStates()
        {
            DataRowView equipment = this.SelectedEquipmentView();
            DataRowView port = this.gridPorts.SelectedItem as DataRowView;
            DataRow equipmentRow = equipment == null ? null : equipment.Row;
            DataRow portRow = port == null ? null : port.Row;
            this.ddbEquipment.DataSource = EquipmentPortManagementPresenter.BuildMenuTable(
                    EquipmentPortManagementPresenter.EquipmentActions, equipmentRow, null,
                    key => EquipmentActionReason(key, equipmentRow, null) == null);
            this.ddbPort.DataSource = EquipmentPortManagementPresenter.BuildMenuTable(
                    EquipmentPortManagementPresenter.PortActions, equipmentRow, portRow,
                    key => EquipmentActionReason(key, equipmentRow, portRow) == null);

            this.ddbEquipment.Enabled = this.newItems.ActionsReady && equipmentRow != null;
            this.ddbPort.Enabled = this.newItems.ActionsReady && portRow != null;

            this.lblTarget.Text = EquipmentPortManagementPresenter.StatusLineText(equipmentRow, portRow);
        }

        private void OnEquipmentActionClicked(object sender, DropDownItemClickedEventArgs e)
        {
            this.ExecuteEquipmentAction(e.Value as string);
        }

        private void OnPortActionClicked(object sender, DropDownItemClickedEventArgs e)
        {
            this.ExecutePortAction(e.Value as string);
        }

        private void ExecuteEquipmentAction(string key)
        {
            DataRowView equipment = this.SelectedEquipmentView();

            if (equipment == null
                    || EquipmentActionReason(key, equipment.Row, null) != null)
            {
                return;
            }

            string eqpId = TableHelper.CellText(equipment.Row, ServerFields.Equipment.EqpId).Trim();

            if (key == EquipmentPortManagementPresenter.ActionAuto)
            {
                bool turnOn = !EquipmentPortManagementForm.IsAuto(equipment.Row);

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
                return;
            }

            if (!this.Confirm(
                    "Change " + eqpId + " from " + EquipmentPortManagementPresenter.Mode(equipment.Row) + " to "
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

        private void ExecutePortAction(string key)
        {
            DataRowView equipment = this.SelectedEquipmentView();
            DataRowView port = this.gridPorts.SelectedItem as DataRowView;

            if (equipment == null || port == null
                    || EquipmentActionReason(key, equipment.Row, port.Row) != null)
            {
                return;
            }

            string eqpId = TableHelper.CellText(equipment.Row, ServerFields.Equipment.EqpId).Trim();
            string portNm = TableHelper.CellText(port.Row, ServerFields.Port.PortNm).Trim();

            if (!this.Confirm(
                    "Change " + eqpId + " port " + portNm
                            + " from " + EquipmentPortManagementPresenter.PortStatus(port.Row) + " to "
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
                    () => this.LoadDependents(eqpId, true));
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

        private void OnMenuEqpOpening(object sender, CancelEventArgs e)
        {
            DataRowView equipment = this.SelectedEquipmentView();
            DataRow equipmentRow = equipment == null ? null : equipment.Row;

            e.Cancel = !ApplyMenuStates(
                    this.menuEqp, EquipmentPortManagementPresenter.EquipmentActions,
                    equipmentRow, null, equipment != null);
        }

        private void OnMenuPortOpening(object sender, CancelEventArgs e)
        {
            DataRowView equipment = this.SelectedEquipmentView();
            DataRowView port = this.gridPorts.SelectedItem as DataRowView;
            DataRow equipmentRow = equipment == null ? null : equipment.Row;
            DataRow portRow = port == null ? null : port.Row;

            e.Cancel = !ApplyMenuStates(
                    this.menuPort, EquipmentPortManagementPresenter.PortActions,
                    equipmentRow, portRow, equipment != null && port != null);
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
                        ApplyActionReason(item, EquipmentActionReason(key, equipment, port));
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

        private void OnPortMenuItemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item != null)
            {
                this.ExecutePortAction(item.Tag as string);
            }
        }
    }
}
