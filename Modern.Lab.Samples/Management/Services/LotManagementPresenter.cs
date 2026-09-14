using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Globalization;
using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.Data;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.Samples.Contracts;

namespace Modern.Lab.Samples.Services
{
    public static class LotManagementPresenter
    {



        public const string MesEnd = "END";

        public const string ActionCreateWafer = "CreateWafer";
        public const string ActionCreateChip = "CreateChip";
        public const string ActionCreateLamella = "CreateLamella";
        public const string ActionHold = "Hold";
        public const string ActionNotOnHold = "NotOnHold";
        public const string ActionChangeSpec = "ChangeSpec";
        public const string ActionScrap = "Scrap";

        public const string DateFormat = "yyyy-MM-dd";

        public const string DurableTypeTray = "TRAY";

        public const string DurableIdColumn = ServerFields.Durable.DurableId;

        public static readonly IList<string> ScreenColumns =
                new List<string> { TableJudgment.IssueColumn, NewItemTracker.ColumnName, NewItemTint.ColumnName }.AsReadOnly();
        public static readonly IList<string> UnitScreenColumns =
                new List<string> { TableJudgment.IssueColumn }.AsReadOnly();
        public static readonly IList<string> ActionKeys =
                new List<string> { ActionCreateChip, ActionCreateLamella, ActionHold,
                    ActionNotOnHold, ActionChangeSpec, ActionScrap }.AsReadOnly();

        public static readonly IList<LotAction> CreateActions =
                new List<LotAction>
                {
                    new LotAction(ActionCreateWafer, "Create Wafer"),
                    new LotAction(ActionCreateChip, "Create Chip"),
                    new LotAction(ActionCreateLamella, "Create Lamella")
                }.AsReadOnly();

        public static readonly IList<LotAction> HoldActions =
                new List<LotAction>
                {
                    new LotAction(ActionHold, "Hold"),
                    new LotAction(ActionNotOnHold, "NotOnHold")
                }.AsReadOnly();

        public static readonly IList<LotAction> ExecuteActions =
                new List<LotAction>
                {
                    new LotAction(ActionChangeSpec, "Change Spec"),
                    new LotAction(ActionScrap, "Scrap")
                }.AsReadOnly();

        public static string Id(DataRow lot)
        {
            return TableHelper.CellText(lot, ServerFields.Lot.LotId).Trim();
        }

        public static string Type(DataRow lot)
        {
            return TableHelper.CellText(lot, ServerFields.Lot.SubProdTyp.Column).Trim();
        }

        public static string Status(DataRow lot)
        {
            return TableHelper.CellText(lot, ServerFields.Lot.LotStatTyp.Column).Trim();
        }

        public static string MesStatus(DataRow lot)
        {
            return TableHelper.CellText(lot, ServerFields.Lot.MesProcStatCd.Column).Trim();
        }

        public static string HoldState(DataRow lot)
        {
            return TableHelper.CellText(lot, ServerFields.Lot.LotHoldStatCd.Column).Trim();
        }

        public static bool IsOnHold(DataRow lot)
        {
            return HoldState(lot) == ServerFields.Lot.LotHoldStatCd.OnHold;
        }




        public static bool IsNotOnHold(DataRow lot)
        {
            return HoldState(lot) == ServerFields.Lot.LotHoldStatCd.NotOnHold;
        }

        public static bool IsActionKey(string key)
        {
            return key == ActionCreateWafer || key == ActionCreateChip || key == ActionCreateLamella
                    || key == ActionHold || key == ActionNotOnHold
                    || key == ActionChangeSpec || key == ActionScrap;
        }

        public static bool CanExecute(string key, DataRow lot)
        {
            if (key == ActionCreateWafer)
            {
                return true;
            }

            if (lot != null && MesStatus(lot) == ServerFields.Lot.MesProcStatCd.PROC)
            {
                return false;
            }

            switch (key)
            {
                case ActionCreateChip:
                    return lot != null && Type(lot) == ServerFields.Lot.SubProdTyp.Wafer && IsReleasedWait(lot) && !IsOnHold(lot);

                case ActionCreateLamella:
                    return lot != null && (Type(lot) == ServerFields.Lot.SubProdTyp.Wafer || Type(lot) == ServerFields.Lot.SubProdTyp.Chip) && IsReleasedWait(lot) && !IsOnHold(lot);

                case ActionHold:
                    return lot != null && Status(lot) != ServerFields.Lot.LotStatTyp.Scrapped && !IsOnHold(lot);

                case ActionNotOnHold:
                    return lot != null && IsOnHold(lot);

                case ActionChangeSpec:
                case ActionScrap:
                    return lot != null && IsReleasedWait(lot) && !IsOnHold(lot);

                default:
                    return false;
            }
        }

        public static bool IsReleasedWait(DataRow lot)
        {
            return Status(lot) == ServerFields.Lot.LotStatTyp.Released && MesStatus(lot) == ServerFields.Lot.MesProcStatCd.WAIT;
        }

        public static DataTable BuildMenuTable(IList<LotAction> actions, DataRow lot)
        {
            return BuildGatedMenuTable(actions, key => CanExecute(key, lot));
        }

        public static DataTable BuildGatedMenuTable(IList<LotAction> actions, Func<string, bool> canExecute)
        {
            DataTable table = new DataTable();
            table.Locale = CultureInfo.InvariantCulture;
            table.Columns.Add("VALUE", typeof(string));
            table.Columns.Add("LABEL", typeof(string));
            table.Columns.Add("CAN", typeof(bool));

            foreach (LotAction action in actions)
            {
                table.Rows.Add(action.Key, action.Label, canExecute(action.Key));
            }

            return table;
        }

        public static string ChildTypeOf(string key)
        {
            return key == ActionCreateChip ? ServerFields.Lot.SubProdTyp.Chip : ServerFields.Lot.SubProdTyp.Lamella;
        }

        public static string TypeFilter(object[] checkedValues)
        {
            if (checkedValues == null || checkedValues.Length == 0)
            {
                return string.Empty;
            }

            List<string> types = new List<string>();

            foreach (object value in checkedValues)
            {
                string type = (Convert.ToString(value) ?? string.Empty).Trim();

                if (type.Length > 0)
                {
                    types.Add(type);
                }
            }

            return string.Join(",", types.ToArray());
        }

        public static string DateText(DateTime? date)
        {
            return date.HasValue
                    ? date.Value.ToString(DateFormat, CultureInfo.InvariantCulture)
                    : string.Empty;
        }

        public static string UnitListTitle(string type)
        {
            return UnitWord(type) + " List";
        }

        public static string UnitHistoryTitle(string type)
        {
            return UnitWord(type) + " History";
        }

        public static string UnitWord(string type)
        {
            if (type == ServerFields.Lot.SubProdTyp.Chip)
            {
                return ServerFields.Lot.SubProdTyp.Chip;
            }

            if (type == ServerFields.Lot.SubProdTyp.Lamella)
            {
                return ServerFields.Lot.SubProdTyp.Lamella;
            }

            return ServerFields.Lot.SubProdTyp.Wafer;
        }

        public static bool HasJudge(string type)
        {
            return type == ServerFields.Lot.SubProdTyp.Chip || type == ServerFields.Lot.SubProdTyp.Lamella;
        }

        public static string SelectionAfterBind(DataTable lots, string keepLotId)
        {
            if (lots == null || lots.Rows.Count == 0)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(keepLotId))
            {
                foreach (DataRow row in lots.Rows)
                {
                    if (Id(row) == keepLotId && !TableJudgment.IsInvalid(row))
                    {
                        return keepLotId;
                    }
                }
            }

            foreach (DataRow row in lots.Rows)
            {
                if (Id(row).Length > 0 && !TableJudgment.IsInvalid(row))
                {
                    return Id(row);
                }
            }
            return string.Empty;
        }

        public static string ActionLabel(string key)
        {
            foreach (IList<LotAction> actions in new IList<LotAction>[] { CreateActions, HoldActions, ExecuteActions })
            {
                foreach (LotAction action in actions)
                {
                    if (action.Key == key)
                    {
                        return action.Label;
                    }
                }
            }
            return string.Empty;
        }

        public static string ActionReasonText(ActionGate gate, string key)
        {
            if (key == ActionCreateWafer)
            {
                return string.Empty;
            }
            string reason = ContractText.ReasonText(gate, key, null);
            return reason.Length == 0 ? string.Empty : ActionLabel(key) + ": " + reason;
        }

        public static string StatusLineText(DataRow lot, ActionGate gate)
        {
            return TargetText(lot) + ContractText.GroupedReasonSuffix(gate, ActionKeys, ActionLabel, null);
        }

        public static string CreatedLotIdOf(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return null;
            }

            Match match = Regex.Match(message, @"\blot (\S+) created\b");
            return match.Success ? match.Groups[1].Value : null;
        }

        public static string TargetText(DataRow lot)
        {
            if (lot == null)
            {
                return "No lot selected";
            }

            return Id(lot) + " · " + Type(lot) + " · " + Status(lot) + " / " + MesStatus(lot)
                    + " · " + HoldState(lot) + " · " + TableHelper.CellText(lot, ServerFields.Lot.Qty).Trim() + " units";
        }
    }
}
