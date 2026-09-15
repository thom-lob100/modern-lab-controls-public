using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.Data;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.Samples.Contracts;

namespace Modern.Lab.Samples.Services
{

    public sealed class DurableAction
    {
        public DurableAction(string key, string label, bool inMenu, Func<DataRow, bool> enabled)
        {
            this.Key = key;
            this.Label = label;
            this.InMenu = inMenu;
            this.Enabled = enabled;
        }

        public string Key { get; private set; }

        public string Label { get; private set; }

        public bool InMenu { get; private set; }

        public Func<DataRow, bool> Enabled { get; private set; }
    }

    public static class DurableManagementPresenter
    {
        public const string TypeFoup = "FOUP";
        public const string TypeTray = "TRAY";

        public const string StatusEmpty = "Empty";
        public const string StatusPartial = "Partial";
        public const string StatusFull = "Full";

        public const string ModeNormal = "Normal";


        public const string SubTypeWafer = "Wafer";

        public const string SubTypeChip = "Chip";
        public const string SubTypeLamella = "Lamella";

        public const string JudgeSucc = JudgeResult.Succ;
        public const string JudgeFail = JudgeResult.Fail;

        public const string ReqSerialNoColumn = ServerFields.Request.ReqSerialNo;
        public const string JudgeResultColumn = JudgeResult.Column;

        public const string JudgeColorColumn = JudgeResult.ColorColumn;

        public const string TypeColumn = ServerFields.Durable.DurableTyp;

        public const string CategoryTypeColumn = ServerFields.Durable.DurableType;

        public const string TypeIdColumn = "TYPE_ID";

        public const string TypeNameColumn = "TYPE_NM";

        public const string CategoryColumn = "CATEGORY";

        public const string LocationColumn = "LOC_NM";

        public const string TransportTargetColumn = "LOCATION_ID";

        public const string ActionCreate = "Create";
        public const string ActionRfId = "RfId";
        public const string ActionExchange = "Exchange";
        public const string ActionEdit = "Edit";
        public const string ActionTransport = "TransPort";

        public static readonly IList<DurableAction> Actions =
                new List<DurableAction>
                {
                    new DurableAction(ActionCreate, "Create", false, durable => true),
                    new DurableAction(ActionRfId, "RFID", false, durable => durable != null),
                    new DurableAction(ActionExchange, "Exchange", true,
                            durable => durable != null && !IsEmpty(durable) && Type(durable) == TypeFoup),
                    new DurableAction(ActionEdit, "Edit", true, durable => durable != null && !IsEmpty(durable) && !IsForeign(durable)),
                    new DurableAction(ActionTransport, "TransPort", true, durable => durable != null)
                }.AsReadOnly();

        public static readonly IList<DurableAction> MenuActions = MenuOf(Actions);

        public static DurableAction Of(string key)
        {
            foreach (DurableAction action in Actions)
            {
                if (action.Key == key)
                {
                    return action;
                }
            }

            return null;
        }

        public static bool IsActionKey(string key)
        {
            return Of(key) != null;
        }

        public static bool CanExecute(string key, DataRow durable)
        {
            DurableAction action = Of(key);

            return action != null && action.Enabled(durable);
        }

        private static IList<DurableAction> MenuOf(IList<DurableAction> actions)
        {
            List<DurableAction> menu = new List<DurableAction>();

            foreach (DurableAction action in actions)
            {
                if (action.InMenu)
                {
                    menu.Add(action);
                }
            }

            return menu.AsReadOnly();
        }

        public static string Type(DataRow durable)
        {
            return TableHelper.CellText(durable, ServerFields.Durable.DurableTyp).Trim();
        }

        public static string Status(DataRow durable)
        {
            return TableHelper.CellText(durable, ServerFields.Durable.WfLoadStatCd.Column).Trim();
        }

        public static string Id(DataRow durable)
        {
            return TableHelper.CellText(durable, ServerFields.Durable.DurableId).Trim();
        }

        public static bool IsForeign(DataRow durable)
        {
            return !string.IsNullOrWhiteSpace(
                    TableHelper.CellText(durable, ServerFields.Durable.FrFabId));
        }

        public static string Location(DataRow durable)
        {
            return TableHelper.CellText(durable, LocationColumn).Trim();
        }

        public static DataTable TransportTargets(DataTable locations, DataRow durable, string currentLocationId)
        {
            DataTable targets = locations == null ? new DataTable() : locations.Clone();

            if (locations == null || durable == null)
            {
                return targets;
            }

            string current = (currentLocationId ?? string.Empty).Trim();

            foreach (DataRow row in locations.Rows)
            {
                if (current.Length > 0 && TableHelper.CellText(row, TransportTargetColumn).Trim() == current)
                {
                    continue;
                }

                targets.ImportRow(row);
            }

            return targets;
        }

        public static bool IsEmpty(DataRow durable)
        {
            return Status(durable) == ServerFields.Durable.WfLoadStatCd.Empty;
        }

        public static bool IsFull(DataRow durable)
        {
            return Status(durable) == ServerFields.Durable.WfLoadStatCd.Full;
        }

        public static bool HasRoom(DataRow durable)
        {
            string status = Status(durable);
            return status == ServerFields.Durable.WfLoadStatCd.Empty
                    || status == ServerFields.Durable.WfLoadStatCd.Partial;
        }

        public static DataTable ExchangeTargets(DataTable durables, DataRow source)
        {
            return Targets(durables, source, true);
        }

        public static DataTable EditTargets(DataTable durables, DataRow source)
        {
            return Targets(durables, source, false);
        }

        private static DataTable Targets(DataTable durables, DataRow source, bool emptyOnly)
        {
            DataTable targets = durables == null ? new DataTable() : durables.Clone();

            if (durables == null || source == null)
            {
                return targets;
            }

            string type = Type(source);
            string sourceId = Id(source);

            foreach (DataRow row in durables.Rows)
            {
                if (TableJudgment.IsInvalid(row) || Type(row).Length == 0 || Type(row) != type || Id(row) == sourceId)
                {
                    continue;
                }

                if (!emptyOnly
                        && (!CandidateGate.IsInteger(TableHelper.CellText(row, ServerFields.Durable.UseNumcnt))
                                || !CandidateGate.IsInteger(TableHelper.CellText(row, ServerFields.Durable.Capa))))
                {
                    continue;
                }

                if (emptyOnly ? IsEmpty(row) : HasRoom(row))
                {
                    targets.ImportRow(row);
                }
            }

            return targets;
        }

        public static DataTable MarkJudgeColors(DataTable history)
        {
            return JudgeResult.Mark(history);
        }

        public static string CreatedIdOf(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            Match match = Regex.Match(message, @"Durable (\S+) created");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        public static string TargetText(DataRow durable)
        {
            if (durable == null)
            {
                return "No durable selected";
            }

            return Id(durable) + " · " + Status(durable) + " · "
                    + TableHelper.CellText(durable, ServerFields.Durable.UseNumcnt).Trim() + "/" + TableHelper.CellText(durable, ServerFields.Durable.Capa).Trim();
        }

        public static string SelectionAfterBind(DataTable durables, string keepDurableId, string fallbackDurableId)
        {
            string selected = SelectionAfterBind(durables, keepDurableId);
            string keep = (keepDurableId ?? string.Empty).Trim();

            if (keep.Length == 0 || selected == keep)
            {
                return selected;
            }

            return SelectionAfterBind(durables, fallbackDurableId);
        }

        public static string SelectionAfterBind(DataTable durables, string keepDurableId)
        {
            if (durables == null || durables.Rows.Count == 0)
            {
                return string.Empty;
            }

            string keep = (keepDurableId ?? string.Empty).Trim();

            if (keep.Length > 0)
            {
                foreach (DataRow row in durables.Rows)
                {
                    if (!TableJudgment.IsInvalid(row) && Id(row) == keep)
                    {
                        return keep;
                    }
                }
            }

            foreach (DataRow row in durables.Rows)
            {
                if (!TableJudgment.IsInvalid(row))
                {
                    return Id(row);
                }
            }

            return string.Empty;
        }


        public static readonly IList<string> ScreenColumns =
                new List<string> { TableJudgment.IssueColumn, NewItemTracker.ColumnName, NewItemTint.ColumnName }.AsReadOnly();

        public static readonly IList<string> ActionKeys =
                new List<string> { ActionExchange, ActionEdit, ActionRfId, ActionTransport }.AsReadOnly();

        public static string ActionLabel(string key)
        {
            DurableAction action = Of(key);
            return action == null ? string.Empty : action.Label;
        }

        public static string BannerText(
                IList<TableResponse> currents, IList<TableResponse> reserved, ResponseContractSet contracts)
        {
            return ContractText.BannerText(currents, reserved, contracts, ActionKeys, ActionLabel, ScreenColumns);
        }

        public static string ActionReasonText(ActionGate gate, string key)
        {
            string reasons = ContractText.ReasonText(gate, key, null);
            return reasons.Length == 0 ? string.Empty : ActionLabel(key) + ": " + reasons;
        }

        public static string StatusLineText(DataRow durable, ActionGate gate)
        {
            return TargetText(durable) + ContractText.GroupedReasonSuffix(gate, ActionKeys, ActionLabel, null);
        }
    }
}
