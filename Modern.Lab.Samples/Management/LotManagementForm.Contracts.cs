using System;
using System.Collections.Generic;
using System.Data;
using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.Data;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.Samples.Contracts;
using Modern.Lab.Samples.Services;

namespace Modern.Lab.Samples
{
    public partial class LotManagementForm
    {
        private DataTable BindJudged(string tableId, ref TableResponse current, ref TableResponse reserved,
                DataTable incoming, IList<string> screenColumns)
        {
            current = null;
            reserved = null;
            if (incoming == null)
            {
                return null;
            }
            TableReception reception = TableJudgment.Receive(tableId, incoming, Contracts, screenColumns);
            if (reception.IsMissingRequired)
            {
                current = reception.Response;
                return reception.DisplayCopy;
            }
            if (reception.HasReservedColumns)
            {
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
            string lot = ContractText.BannerText(new TableResponse[] { this.lotCurrent },
                    new TableResponse[] { this.lotReserved }, Contracts, LotManagementPresenter.ActionKeys,
                    LotManagementPresenter.ActionLabel, LotManagementPresenter.ScreenColumns);
            string unit = ContractText.BannerText(new TableResponse[] { this.unitCurrent },
                    new TableResponse[] { this.unitReserved }, Contracts, LotManagementPresenter.ActionKeys,
                    LotManagementPresenter.ActionLabel, LotManagementPresenter.UnitScreenColumns);
            string text = lot.Length == 0 ? unit : (unit.Length == 0 ? lot : lot + " · " + unit);
            this.SetContractNotice(text);
        }

        private ActionGate BuildGate(DataRow lot)
        {
            ActionGate gate = new ActionGate(Contracts);
            if (!this.newItems.ActionsReady || this.lotCurrent == null || this.lotData == null)
            {
                return gate;
            }
            int index = lot == null ? -1 : this.lotData.Rows.IndexOf(lot);
            DataRow selected = index < 0 || this.lotCurrent.Table == null || index >= this.lotCurrent.Table.Rows.Count
                    ? null : this.lotCurrent.Table.Rows[index];
            return gate.Observe(LotContracts.LotTable, this.lotCurrent, selected);
        }

        private bool ActionEnabled(string key, DataRow lot, ActionGate gate)
        {
            if (key == LotManagementPresenter.ActionCreateWafer)
            {
                return true;
            }
            return this.newItems.ActionsReady && lot != null
                    && lot.RowState != DataRowState.Deleted && lot.RowState != DataRowState.Detached
                    && gate.CanExecute(key) && LotManagementPresenter.CanExecute(key, lot);
        }

        private bool CanProcess(string key, string lotId)
        {
            if (!this.newItems.ActionsReady || !Judged(this.lotCurrent) || this.lotData == null)
            {
                return false;
            }

            DataTable original = new DataTable(this.lotData.TableName);
            original.Locale = this.lotData.Locale;
            foreach (DataColumn column in this.lotData.Columns)
            {
                if (!string.Equals(column.ColumnName, TableJudgment.IssueColumn, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(column.ColumnName, NewItemTracker.ColumnName, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(column.ColumnName, NewItemTint.ColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    original.Columns.Add(column.ColumnName, column.DataType);
                }
            }
            foreach (DataRow row in this.lotData.Rows)
            {
                if (row.RowState == DataRowState.Deleted || row.RowState == DataRowState.Detached)
                {
                    continue;
                }
                DataRow copy = original.NewRow();
                foreach (DataColumn column in original.Columns)
                {
                    copy[column.ColumnName] = row[column.ColumnName];
                }
                original.Rows.Add(copy);
            }
            TableResponse current = TableResponse.Read(ResponseKind.Data, original, Contracts.Aliases,
                    Contracts.TableFor(LotContracts.LotTable));
            DataRow target = CandidateGate.Find(current, ServerFields.Lot.LotId, lotId);
            return new ActionGate(Contracts).Observe(LotContracts.LotTable, current, target).CanExecute(key)
                    && LotManagementPresenter.CanExecute(key, target);
        }
    }
}
