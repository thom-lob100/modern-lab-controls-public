using System;
using System.Collections.Generic;
using System.Data;
using Modern.Lab.Data;
using Modern.Lab.WinForms.Controls.Display;
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.Samples.Contracts;

namespace Modern.Lab.Samples.Services
{

    public static class EquipmentLotPresenter
    {
        public const string ActionJobPrep = "JobPrep";
        public const string ActionJobStart = "JobStart";
        public const string ActionJobEnd = "JobEnd";

        public const string SummaryInPort = "IN_PORT";
        public const string SummaryOutPort = "OUT_PORT";

        public const string SummaryGoalDurable = "GOAL_DURABLE_ID";

        public const string JobStatePrep = "JobPrep";
        public const string JobStateStart = "JobStart";
        public const string JobStateEnd = "JobEnd";


        public const string JobColorColumn = "JOB_COLOR";
        public const string AutoCanColumn = "AUTO_CAN";
        public const string PriorityColorColumn = "PRIORITY_COLOR";

        public const string ServerOrderColumn = "SERVER_ORDER";

        public const string PrioritySpinColumn = "PRIORITY_SPIN";

        public static readonly IList<string> ScreenColumns = new List<string>
        {
            AutoCanColumn, JobColorColumn, PriorityColorColumn, PrioritySpinColumn, ServerOrderColumn, TableJudgment.IssueColumn
        }.AsReadOnly();

        public const int DefaultIntervalSeconds = 15;
        public static readonly int[] IntervalChoices = new int[] { 0, 5, 10, 15, 30, 60 };

        public const int CountdownSeconds = 5;

        public static readonly IList<LotAction> JobActions =
                new List<LotAction>
                {
                    new LotAction(ActionJobPrep, "Job Prep"),
                    new LotAction(ActionJobStart, "Job Start"),
                    new LotAction(ActionJobEnd, "Job End")
                }.AsReadOnly();

        public static DataTable IntervalOptions()
        {
            DataTable table = new DataTable();
            table.Locale = System.Globalization.CultureInfo.InvariantCulture;
            table.Columns.Add("SECONDS", typeof(int));
            table.Columns.Add("LABEL", typeof(string));

            foreach (int seconds in IntervalChoices)
            {
                table.Rows.Add(seconds, seconds == 0 ? "Off" : seconds.ToString() + " s");
            }

            return table;
        }

        public static string CountdownText(int secondsLeft, int intervalSeconds)
        {
            if (intervalSeconds <= 0 || secondsLeft <= 0 || secondsLeft > CountdownSeconds)
            {
                return string.Empty;
            }

            return "Refresh in " + secondsLeft + " s";
        }

        public static int IntervalSeconds(object selectedValue)
        {
            return TableHelper.ParseInt(Convert.ToString(selectedValue) ?? string.Empty);
        }

        public static string JobColorSignature(DataTable lots)
        {
            if (lots == null)
            {
                return string.Empty;
            }

            System.Text.StringBuilder signature = new System.Text.StringBuilder();

            foreach (DataRow row in lots.Rows)
            {
                signature.Append(TableHelper.CellText(row, ServerFields.Lot.LotId)).Append('=').Append(JobState(row)).Append(';');
            }

            return signature.ToString();
        }

        public static bool IsJobKey(string key)
        {
            return key == ActionJobPrep || key == ActionJobStart || key == ActionJobEnd;
        }

        public static bool IsCandidate(DataRow row)
        {
            return Priority(row) > 0 && !TableJudgment.IsInvalid(row);
        }

        public static int Priority(DataRow row)
        {
            return TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Priority));
        }

        public static string JobState(DataRow lot)
        {
            return JobDecision.IsLive(lot) ? TableHelper.CellText(lot, ServerFields.Lot.LastEventCd.Column).Trim() : string.Empty;
        }

        public static string PortType(DataRow port)
        {
            return TableHelper.CellText(port, ServerFields.Port.PortTyp.Column).Trim();
        }

        public static string LotTypeOf(DataRow group)
        {
            return TableHelper.CellText(group, ServerFields.Lot.SubProdTyp.Column).Trim();
        }

        public static string PortDurableType(DataRow port)
        {
            return TableHelper.CellText(port, ServerFields.Port.DurableType).Trim();
        }

        public static string JobOutPortNm(DataRow lot)
        {
            return HasActiveJob(lot) ? TableHelper.CellText(lot, ServerFields.Lot.GoalPortNm).Trim() : string.Empty;
        }

        public static string DurableListTitle(string durableType)
        {
            return durableType.Length == 0 ? "Durable List (Target)" : "Durable List (Target) — " + durableType;
        }

        public static DataRow TopPriority(DataTable table)
        {
            if (table == null || table.Rows.Count == 0)
            {
                return null;
            }

            DataRow best = null;

            foreach (DataRow row in table.Rows)
            {
                if (!IsCandidate(row))
                {
                    continue;
                }

                if (Priority(row) == 1)
                {
                    return row;
                }

                if (best == null || Priority(row) < Priority(best))
                {
                    best = row;
                }
            }

            return best;
        }

        public static DataRow TopPort(DataTable ports, string type)
        {
            if (ports == null)
            {
                return null;
            }

            DataRow best = null;

            foreach (DataRow row in ports.Rows)
            {
                string portType = PortType(row);

                if (portType != type && portType != ServerFields.Port.PortTyp.InputOutput)
                {
                    continue;
                }

                if (!IsCandidate(row))
                {
                    continue;
                }

                if (best == null || Priority(row) < Priority(best))
                {
                    best = row;
                }
            }

            return best;
        }

        public static DataRow FindById(DataTable table, string column, string id)
        {
            if (table == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (DataRow row in table.Rows)
            {
                if (TableHelper.CellText(row, column).Trim() == id)
                {
                    return row;
                }
            }

            return null;
        }

        public static bool HasActiveJob(DataRow lot)
        {
            string state = JobState(lot);
            return state == JobStatePrep || state == JobStateStart;
        }

        public static DataRow JobEquipment(JobDecision decision)
        {
            if (decision == null)
            {
                return null;
            }

            if (!HasActiveJob(decision.Lot))
            {
                return decision.Equipment;
            }

            string eqpId = TableHelper.CellText(decision.Lot, ServerFields.Lot.EqpId).Trim();

            if (eqpId.Length == 0)
            {
                return null;
            }

            if (decision.Equipment != null
                    && TableHelper.CellText(decision.Equipment, ServerFields.Equipment.EqpId).Trim() == eqpId)
            {
                return decision.Equipment;
            }

            return FindById(decision.EquipmentList, ServerFields.Equipment.EqpId, eqpId);
        }

        public static bool PortsCompatible(DataRow inPort, DataRow outPort)
        {
            if (inPort == null || outPort == null)
            {
                return false;
            }

            string inType = PortType(inPort);
            string outType = PortType(outPort);

            if (TableHelper.CellText(inPort, ServerFields.Port.PortNm).Trim() == TableHelper.CellText(outPort, ServerFields.Port.PortNm).Trim())
            {
                return inType == ServerFields.Port.PortTyp.InputOutput;
            }

            return (inType == ServerFields.Port.PortTyp.Input || inType == ServerFields.Port.PortTyp.InputOutput)
                    && (outType == ServerFields.Port.PortTyp.Output || outType == ServerFields.Port.PortTyp.InputOutput);
        }

        public static bool CanExecute(string key, JobDecision decision)
        {
            if (decision == null || !IsJobKey(key))
            {
                return false;
            }

            DataRow equipment = JobEquipment(decision);

            if (equipment == null)
            {
                return false;
            }

            string mode = EquipmentPortManagementPresenter.Mode(equipment);

            if (mode != ServerFields.Equipment.CommStatTyp.OnLineRemote
                    && mode != ServerFields.Equipment.CommStatTyp.OnLineLocal)
            {
                return false;
            }

            DataRow lot = decision.Lot;

            if (lot != null && !LotManagementPresenter.IsNotOnHold(lot))
            {
                return false;
            }

            switch (key)
            {
                case ActionJobPrep:
                    return lot != null && !HasActiveJob(lot)
                            && LotManagementPresenter.IsReleasedWait(lot)
                            && PortsCompatible(decision.InPort, decision.OutPort)
                            && EquipmentPortManagementPresenter.PortStatus(decision.InPort) == ServerFields.Port.TransferStatCd.ReadyToUnload
                            && EquipmentPortManagementPresenter.PortStatus(decision.OutPort) == ServerFields.Port.TransferStatCd.ReadyToUnload
                            && decision.Durable != null
                            && DurableManagementPresenter.IsEmpty(decision.Durable);

                case ActionJobStart:
                    return mode == ServerFields.Equipment.CommStatTyp.OnLineLocal && JobState(lot) == JobStatePrep;

                case ActionJobEnd:
                    return mode == ServerFields.Equipment.CommStatTyp.OnLineLocal && JobState(lot) == JobStateStart;

                default:
                    return false;
            }
        }

        public static DataRow JobSummary(JobDecision decision)
        {
            DataTable table = new DataTable();
            table.Columns.Add(ServerFields.Equipment.EqpId);
            table.Columns.Add(ServerFields.Equipment.CommStatTyp.Column);
            table.Columns.Add(SummaryInPort);
            table.Columns.Add(SummaryOutPort);
            table.Columns.Add(ServerFields.Lot.LotId);
            table.Columns.Add(ServerFields.Durable.DurableId);
            table.Columns.Add(SummaryGoalDurable);
            table.Columns.Add(ServerFields.Durable.DurableTyp);
            table.Columns.Add(ServerFields.Lot.LastEventCd.Column);

            DataRow row = table.NewRow();

            if (decision != null)
            {
                DataRow lot = decision.Lot;
                bool job = HasActiveJob(lot);
                DataRow equipment = JobEquipment(decision);

                row[ServerFields.Equipment.EqpId] = job ? TableHelper.CellText(lot, ServerFields.Lot.EqpId).Trim() : decision.EqpId;
                row[ServerFields.Equipment.CommStatTyp.Column] = equipment == null
                        ? string.Empty
                        : EquipmentPortManagementPresenter.Mode(equipment);
                row[SummaryInPort] = job ? TableHelper.CellText(lot, ServerFields.Lot.PortNm).Trim() : decision.InPortNm;
                row[SummaryOutPort] = job ? TableHelper.CellText(lot, ServerFields.Lot.GoalPortNm).Trim() : decision.OutPortNm;
                row[ServerFields.Lot.LotId] = decision.LotId;
                row[ServerFields.Durable.DurableId] = TableHelper.CellText(lot, ServerFields.Lot.CarrierId).Trim();
                row[SummaryGoalDurable] = job
                        ? TableHelper.CellText(lot, ServerFields.Lot.GoalCarrierId).Trim()
                        : decision.DurableId;
                row[ServerFields.Durable.DurableTyp] = decision.Durable == null
                        ? string.Empty
                        : TableHelper.CellText(decision.Durable, ServerFields.Durable.DurableTyp).Trim();
                row[ServerFields.Lot.LastEventCd.Column] = JobState(lot);
            }

            table.Rows.Add(row);
            return row;
        }

        public static ModernFieldDefinition[] JobSummaryFields()
        {
            return new ModernFieldDefinition[]
            {
                new ModernFieldDefinition(ServerFields.Equipment.EqpId),
                new ModernFieldDefinition(ServerFields.Equipment.CommStatTyp.Column),
                new ModernFieldDefinition(SummaryInPort),
                new ModernFieldDefinition(SummaryOutPort),
                new ModernFieldDefinition(ServerFields.Lot.LotId),
                new ModernFieldDefinition(ServerFields.Durable.DurableId),
                new ModernFieldDefinition(SummaryGoalDurable),
                new ModernFieldDefinition("Durable Type", ServerFields.Durable.DurableTyp),
                new ModernFieldDefinition(ServerFields.Lot.LastEventCd.Column)
            };
        }

        public static string JobLabel(string key)
        {
            switch (key)
            {
                case ActionJobPrep:
                    return "Job Prep";

                case ActionJobStart:
                    return "Job Start";

                case ActionJobEnd:
                    return "Job End";

                default:
                    return string.Empty;
            }
        }

        public const string PriorityFirstColor = "#F87171";
        public const string PrioritySecondColor = "#FB923C";
        public const string PriorityThirdColor = "#FACC15";
        public const string PriorityNextColor = "#F2E3A6";
        public const string PriorityRestColor = "#F3F4F6";

        public static string PriorityColor(int priority)
        {
            switch (priority)
            {
                case 1:
                    return PriorityFirstColor;

                case 2:
                    return PrioritySecondColor;

                case 3:
                    return PriorityThirdColor;

                case 4:
                case 5:
                    return PriorityNextColor;

                default:
                    return PriorityRestColor;
            }
        }

        public static DataTable MarkPriorityColors(DataTable table)
        {
            if (table == null || !table.Columns.Contains(ServerFields.Priority))
            {
                return table;
            }

            if (!table.Columns.Contains(PriorityColorColumn))
            {
                table.Columns.Add(PriorityColorColumn, typeof(string));
            }

            foreach (DataRow row in table.Rows)
            {
                row[PriorityColorColumn] = IsCandidate(row) ? PriorityColor(Priority(row)) : string.Empty;
            }

            return table;
        }

        public static DataTable MarkServerOrder(DataTable table, DataTable source, string keyColumn)
        {
            if (table == null)
            {
                return null;
            }

            if (!table.Columns.Contains(ServerOrderColumn))
            {
                table.Columns.Add(ServerOrderColumn, typeof(int));
            }

            if (source == null || string.IsNullOrEmpty(keyColumn))
            {
                for (int index = 0; index < table.Rows.Count; index++)
                {
                    SetServerOrder(table.Rows[index], index);
                }

                return table;
            }

            Dictionary<string, int> order = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int index = 0; index < source.Rows.Count; index++)
            {
                string key = TableHelper.CellText(source.Rows[index], keyColumn).Trim();

                if (key.Length > 0 && !order.ContainsKey(key))
                {
                    order.Add(key, index);
                }
            }

            foreach (DataRow row in table.Rows)
            {
                int position;

                if (order.TryGetValue(TableHelper.CellText(row, keyColumn).Trim(), out position))
                {
                    SetServerOrder(row, position);
                }
            }

            return table;
        }

        private static void SetServerOrder(DataRow row, int position)
        {
            if (row.IsNull(ServerOrderColumn) || Convert.ToInt32(row[ServerOrderColumn]) != position)
            {
                row[ServerOrderColumn] = position;
            }
        }

        public static string LotFlowOper(DataRow lot)
        {
            if (!JobDecision.IsLive(lot))
            {
                return string.Empty;
            }

            string flow = TableHelper.CellText(lot, "FLOW_ID").Trim();
            string oper = TableHelper.CellText(lot, "OPER_ID").Trim();

            if (flow.Length > 0 && oper.Length > 0)
            {
                return flow + " · " + oper;
            }

            return flow.Length > 0 ? flow : oper;
        }

        public static DataTable MarkAutoCan(DataTable equipments)
        {
            if (equipments == null)
            {
                return null;
            }

            if (!equipments.Columns.Contains(AutoCanColumn))
            {
                equipments.Columns.Add(AutoCanColumn, typeof(bool));
            }

            foreach (DataRow row in equipments.Rows)
            {
                row[AutoCanColumn] = EquipmentPortManagementPresenter.CanExecute(EquipmentPortManagementPresenter.ActionAuto, row, null)
                        && !TableJudgment.IsInvalid(row);
            }

            return equipments;
        }

        public static DataTable MarkJobColors(DataTable lots)
        {
            if (lots == null)
            {
                return null;
            }

            if (!lots.Columns.Contains(JobColorColumn))
            {
                lots.Columns.Add(JobColorColumn, typeof(string));
            }

            if (!lots.Columns.Contains(PrioritySpinColumn))
            {
                lots.Columns.Add(PrioritySpinColumn, typeof(string));
            }

            System.Drawing.Color surface = Modern.Lab.Theming.ModernTokenColors.Get(
                    "Brush.Surface", System.Drawing.Color.White);
            string runColor = System.Drawing.ColorTranslator.ToHtml(Modern.Lab.Theming.ModernTokenColors.Blend(
                    Modern.Lab.Theming.ModernTokenColors.Get("Brush.Accent", Modern.Lab.Theming.ModernTheme.Accent), surface, 0.16));
            string prepColor = System.Drawing.ColorTranslator.ToHtml(Modern.Lab.Theming.ModernTokenColors.Blend(
                    Modern.Lab.Theming.ModernTokenColors.Get("Brush.WarningBorder", System.Drawing.Color.FromArgb(217, 119, 6)), surface, 0.18));

            foreach (DataRow row in lots.Rows)
            {
                string state = JobState(row);
                row[JobColorColumn] = state == JobStateStart ? runColor : (state == JobStatePrep ? prepColor : string.Empty);
                row[PrioritySpinColumn] = state == JobStateStart ? "Y" : string.Empty;
            }

            return lots;
        }

        public static string TargetText(JobDecision decision)
        {
            DataRow equipment = JobEquipment(decision);

            if (equipment == null)
            {
                return "No equipment decided";
            }

            string text = TableHelper.CellText(equipment, ServerFields.Equipment.EqpId).Trim()
                    + " · " + EquipmentPortManagementPresenter.Mode(equipment);

            if (decision.InPort != null || decision.OutPort != null)
            {
                text = text + " · Port " + (decision.InPortNm.Length > 0 ? decision.InPortNm : "?")
                        + " → " + (decision.OutPortNm.Length > 0 ? decision.OutPortNm : "?");
            }

            if (decision.Lot != null)
            {
                text = text + " · " + decision.LotId;
                string state = JobState(decision.Lot);

                if (state.Length > 0)
                {
                    text = text + " (" + state + ")";
                }
            }

            if (decision.Durable != null)
            {
                text = text + " · " + decision.DurableId;
            }

            return decision.Locked ? text + " · Locked" : text;
        }

        public static readonly IList<string> JobKeys =
                new List<string> { ActionJobPrep, ActionJobStart, ActionJobEnd }.AsReadOnly();

        public static string BannerText(
                IList<TableResponse> currents, IList<TableResponse> reserved, ResponseContractSet contracts)
        {
            return ContractText.BannerText(currents, reserved, contracts, JobKeys, JobLabel, ScreenColumns);
        }

        public static string StatusLineText(JobDecision decision, ActionGate gate)
        {
            string target = TargetText(decision);
            List<string> parts = new List<string>();

            foreach (string key in new string[] { ActionJobPrep, ActionJobStart, ActionJobEnd })
            {
                string text = ActionReasonText(gate, key, decision);

                if (text.Length > 0)
                {
                    parts.Add(text);
                }
            }

            return parts.Count == 0 ? target : target + " — " + string.Join(" · ", parts.ToArray());
        }

        public static string ActionReasonText(ActionGate gate, string key, JobDecision decision)
        {
            if (gate == null || !IsJobKey(key) || !StageApplies(key, decision))
            {
                return string.Empty;
            }

            string reasons = ContractText.ReasonText(gate, key, EquipmentLotContracts.SlotLabel);
            return reasons.Length == 0 ? string.Empty : JobLabel(key) + ": " + reasons;
        }

        public static bool StageApplies(string key, JobDecision decision)
        {
            DataRow lot = decision == null ? null : decision.Lot;
            string state = JobState(lot);

            switch (key)
            {
                case ActionJobPrep:
                    return lot == null || !HasActiveJob(lot);

                case ActionJobStart:
                    return state == JobStatePrep;

                case ActionJobEnd:
                    return state == JobStateStart;

                default:
                    return false;
            }
        }
    }
}
