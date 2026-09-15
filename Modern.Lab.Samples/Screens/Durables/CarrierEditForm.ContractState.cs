using System;
using System.Data;
using Modern.Lab.Controls.Wpf.Display;
using Modern.Lab.Data;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.Samples.Contracts;
using Modern.Lab.Samples.Services;

namespace Modern.Lab.Samples
{
    public partial class CarrierEditForm
    {
        private static readonly ResponseContractSet Contracts = CarrierEditContracts.Build();

        private TableResponse carrierCurrent;
        private TableResponse sourceMapCurrent;
        private TableResponse targetMapCurrent;

        private string carrierListType = string.Empty;
        private string sourceMapType = string.Empty;
        private string sourceMapCarrierId = string.Empty;
        private string targetMapType = string.Empty;
        private string targetMapCarrierId = string.Empty;

        private void ResetActionContractState()
        {
            this.carrierCurrent = null;
            this.carrierListType = string.Empty;
            this.ResetSourceMapContractState();
            this.ResetTargetMapContractState();
            this.SetContractNotice(string.Empty);
        }

        private void ResetSourceMapContractState()
        {
            this.sourceMapCurrent = null;
            this.sourceMapType = string.Empty;
            this.sourceMapCarrierId = string.Empty;
        }

        private void ResetTargetMapContractState()
        {
            this.targetMapCurrent = null;
            this.targetMapType = string.Empty;
            this.targetMapCarrierId = string.Empty;
        }

        private void BeginCarrierListContractLoad(string type)
        {
            this.carrierCurrent = null;
            this.carrierListType = type ?? string.Empty;
            this.ResetSourceMapContractState();
            this.ResetTargetMapContractState();
            this.RefreshContractNotice();
        }

        private void BeginSourceMapContractLoad(string type, string carrierId)
        {
            this.sourceMapSettled = false;
            this.sourceMapCurrent = null;
            this.sourceMapType = type ?? string.Empty;
            this.sourceMapCarrierId = carrierId ?? string.Empty;
            this.RefreshContractNotice();
        }

        private void BeginTargetMapContractLoad(string type, string carrierId)
        {
            this.targetMapSettled = false;
            this.targetMapCurrent = null;
            this.targetMapType = type ?? string.Empty;
            this.targetMapCarrierId = carrierId ?? string.Empty;
            this.RefreshContractNotice();
        }

        private DataTable BindCarrierResponse(DataTable incoming, string type)
        {
            this.carrierCurrent = TableResponse.Read(
                    ResponseKind.Data,
                    incoming ?? new DataTable(),
                    Contracts.Aliases,
                    Contracts.TableFor(CarrierEditContracts.CarrierTable));
            this.carrierListType = type ?? string.Empty;
            this.RefreshContractNotice();

            return this.carrierCurrent.State == TableResponseState.MissingRequired
                    ? DeclaredEmpty(CarrierEditContracts.CarrierTable)
                    : this.carrierCurrent.Table;
        }

        private DataTable BindMapResponse(
                string tableId,
                DataTable incoming,
                string type,
                string carrierId,
                Modern.Lab.WinForms.Controls.Selection.ModernComboBox combo)
        {
            bool schemaWasAbsent = incoming == null || incoming.Columns.Count == 0;
            DataTable prepared = CarrierEditPresenter.PrepareMap(incoming);
            TableResponse received = null;

            if (prepared.Rows.Count > 0 || !schemaWasAbsent)
            {
                received = TableResponse.Read(
                        ResponseKind.Data,
                        prepared,
                        Contracts.Aliases,
                        Contracts.TableFor(tableId));
            }

            if (received != null && received.State == TableResponseState.MissingRequired)
            {
                this.SetMapCurrent(tableId, received, type, carrierId);
                this.RefreshContractNotice();
                return SlotMapWaferTable.CreateEmpty();
            }

            if (received != null
                    && received.State == TableResponseState.Empty
                    && !HasCompleteMapSchema(Contracts.TableFor(tableId), received.Table))
            {
                TableResponse missingSchema = TableResponse.Read(
                        ResponseKind.Data,
                        prepared,
                        Contracts.Aliases,
                        RequiredMapSchemaContract(Contracts.TableFor(tableId)));
                this.SetMapCurrent(tableId, missingSchema, type, carrierId);
                this.RefreshContractNotice();
                return SlotMapWaferTable.CreateEmpty();
            }

            if (schemaWasAbsent && !this.SelectedCarrierIsEmpty(combo))
            {
                DataTable inconsistent = new DataTable();
                inconsistent.Columns.Add("SparseMap", typeof(string));
                TableResponse blocked = TableResponse.Read(
                        ResponseKind.Data,
                        inconsistent,
                        Contracts.Aliases,
                        RequiredMapSchemaContract(Contracts.TableFor(tableId)));
                this.SetMapCurrent(tableId, blocked, type, carrierId);
                this.RefreshContractNotice();
                return SlotMapWaferTable.CreateEmpty();
            }

            DataTable normalized = this.NormalizeWafers(prepared, combo);
            TableResponse full = TableResponse.Read(
                    ResponseKind.Data,
                    CarrierEditPresenter.PrepareMap(normalized),
                    Contracts.Aliases,
                    Contracts.TableFor(tableId));
            TableResponse current = received == null || received.State == TableResponseState.Empty
                    ? full
                    : received;

            this.SetMapCurrent(tableId, current, type, carrierId);
            this.RefreshContractNotice();
            return full.Table;
        }

        private static bool HasCompleteMapSchema(TableContract contract, DataTable table)
        {
            if (contract == null || table == null)
            {
                return false;
            }

            foreach (string column in contract.DeclaredNames)
            {
                if (!table.Columns.Contains(column))
                {
                    return false;
                }
            }

            return true;
        }

        private bool SelectedCarrierIsEmpty(
                Modern.Lab.WinForms.Controls.Selection.ModernComboBox combo)
        {
            DataRowView selected = combo == null ? null : combo.SelectedItem as DataRowView;
            return selected != null
                    && CarrierEditPresenter.IsValidCarrier(selected.Row)
                    && SelectedCarrierSlotCount(selected.Row) == 0;
        }

        private static int SelectedCarrierSlotCount(DataRow row)
        {
            string type = TableHelper.CellText(row, ServerFields.Carrier.DurableTyp).Trim();

            if (type == ServerFields.Carrier.Foup)
            {
                return TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.UseFoupCount));
            }

            return TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.UseStubCount))
                    + TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.UseLccCount));
        }

        private static TableContract RequiredMapSchemaContract(TableContract contract)
        {
            string[] columns = new string[contract.DeclaredNames.Count];

            for (int index = 0; index < contract.DeclaredNames.Count; index++)
            {
                columns[index] = contract.DeclaredNames[index];
            }

            return new TableContract(contract.TableId).Require(columns);
        }

        private void SetMapCurrent(
                string tableId,
                TableResponse current,
                string type,
                string carrierId)
        {
            if (tableId == CarrierEditContracts.SourceMapTable)
            {
                this.sourceMapCurrent = current;
                this.sourceMapType = type ?? string.Empty;
                this.sourceMapCarrierId = carrierId ?? string.Empty;
            }
            else
            {
                this.targetMapCurrent = current;
                this.targetMapType = type ?? string.Empty;
                this.targetMapCarrierId = carrierId ?? string.Empty;
            }
        }

        private void SetCarrierFailure(string type, Exception failure)
        {
            this.carrierCurrent = Failure(CarrierEditContracts.CarrierTable, failure);
            this.carrierListType = type ?? string.Empty;
            this.ResetSourceMapContractState();
            this.ResetTargetMapContractState();
            this.RefreshContractNotice();
        }

        private void SetMapFailure(string tableId, string type, string carrierId, Exception failure)
        {
            TableResponse current = Failure(tableId, failure);

            if (tableId == CarrierEditContracts.SourceMapTable)
            {
                this.sourceMapCurrent = current;
                this.sourceMapType = type ?? string.Empty;
                this.sourceMapCarrierId = carrierId ?? string.Empty;
            }
            else
            {
                this.targetMapCurrent = current;
                this.targetMapType = type ?? string.Empty;
                this.targetMapCarrierId = carrierId ?? string.Empty;
            }

            this.RefreshContractNotice();
        }

        private bool ActionShapeAllows(string action)
        {
            if (this.carrierCurrent == null
                    || this.carrierCurrent.DisqualifiedRows.Count > 0
                    || this.sourceMapCurrent == null
                    || this.sourceMapCurrent.DisqualifiedRows.Count > 0)
            {
                return false;
            }

            if (action == CarrierEditPresenter.ActionMove
                    && (this.targetMapCurrent == null
                        || this.targetMapCurrent.DisqualifiedRows.Count > 0
                        || this.targetMapCurrent.State == TableResponseState.Failed))
            {
                return false;
            }

            return this.BuildActionGate().CanExecute(action);
        }

        private bool CanExecuteCarrierAction(string action)
        {
            return this.CanExecuteCarrierAction(action, this.stagedWafers);
        }

        private bool CanExecuteCarrierAction(string action, DataTable staged)
        {
            if (this.SourceLocked && action == CarrierEditPresenter.ActionScrap)
            {
                return false;
            }

            return this.CanStartAction()
                    && this.ActionShapeAllows(action)
                    && CarrierEditPresenter.CanExecute(
                            action,
                            this.GetSelectedType(),
                            this.SourceId(),
                            this.TargetId(),
                            this.sourceData,
                            this.targetData,
                            staged);
        }

        private string CarrierActionReason(string action)
        {
            if (this.SourceLocked && action == CarrierEditPresenter.ActionScrap)
            {
                return "Scrap is not available from Durable Edit.";
            }

            string business = CarrierEditPresenter.ActionReason(
                    action,
                    this.GetSelectedType(),
                    this.SourceId(),
                    this.TargetId(),
                    this.sourceData,
                    this.targetData,
                    this.stagedWafers);

            if (business.Length > 0)
            {
                return business;
            }

            return this.ActionShapeAllows(action)
                    ? string.Empty
                    : "Carrier response contract is not ready.";
        }

        private ActionGate BuildActionGate()
        {
            ActionGate gate = new ActionGate(Contracts);
            string type = this.GetSelectedType();
            string sourceId = this.SourceId();
            string targetId = this.TargetId();

            if (this.carrierCurrent != null
                    && string.Equals(this.carrierListType, type, StringComparison.Ordinal))
            {
                gate.Observe(
                        CarrierEditContracts.CarrierTable,
                        this.carrierCurrent,
                        SelectionSlot.Of(
                                CarrierEditContracts.SourceSlot,
                                FindCarrierRow(this.carrierCurrent, sourceId)),
                        SelectionSlot.Of(
                                CarrierEditContracts.TargetSlot,
                                FindCarrierRow(this.carrierCurrent, targetId)));
            }

            if (this.sourceMapCurrent != null
                    && string.Equals(this.sourceMapType, type, StringComparison.Ordinal)
                    && string.Equals(this.sourceMapCarrierId, sourceId, StringComparison.Ordinal))
            {
                gate.Observe(
                        CarrierEditContracts.SourceMapTable,
                        this.sourceMapCurrent,
                        FirstRow(this.sourceMapCurrent));
            }

            if (this.targetMapCurrent != null
                    && string.Equals(this.targetMapType, type, StringComparison.Ordinal)
                    && string.Equals(this.targetMapCarrierId, targetId, StringComparison.Ordinal))
            {
                gate.Observe(
                        CarrierEditContracts.TargetMapTable,
                        this.targetMapCurrent,
                        FirstRow(this.targetMapCurrent));
            }

            return gate;
        }

        private void RefreshContractNotice()
        {
            string text = ContractText.BannerText(
                    new TableResponse[] { this.carrierCurrent, this.sourceMapCurrent, this.targetMapCurrent },
                    new TableResponse[] { null, null, null },
                    Contracts,
                    CarrierEditPresenter.ActionKeys,
                    CarrierEditPresenter.ActionLabel,
                    CarrierEditPresenter.ScreenColumns);
            this.SetContractNotice(text);
        }

        private static TableResponse Failure(string tableId, Exception failure)
        {
            return TableResponse.Read(
                    ResponseKind.Failed,
                    null,
                    Contracts.Aliases,
                    Contracts.TableFor(tableId),
                    string.Empty,
                    failure == null ? "Carrier query failed." : failure.Message);
        }

        private static DataTable DeclaredEmpty(string tableId)
        {
            return TableResponse.Read(
                    ResponseKind.Data,
                    new DataTable(),
                    Contracts.Aliases,
                    Contracts.TableFor(tableId)).Table;
        }

        private static DataRow FindCarrierRow(TableResponse response, string carrierId)
        {
            if (response == null || response.Table == null || string.IsNullOrEmpty(carrierId))
            {
                return null;
            }

            foreach (DataRow row in response.Table.Rows)
            {
                if (string.Equals(
                        TableResponse.CellText(row, ServerFields.Durable.DurableId),
                        carrierId,
                        StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }

        private static DataRow FirstRow(TableResponse response)
        {
            return response == null || response.Table == null || response.Table.Rows.Count == 0
                    ? null
                    : response.Table.Rows[0];
        }
    }
}
