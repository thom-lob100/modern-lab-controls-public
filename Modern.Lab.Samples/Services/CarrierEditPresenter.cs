using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Modern.Lab.Data;
using Modern.Lab.Samples.Contracts;

using Modern.Lab.Hosting.Contracts;
namespace Modern.Lab.Samples.Services
{
    internal static class CarrierEditPresenter
    {
        internal const string ActionMove = "Move";
        internal const string ActionScrap = "Scrap";
        internal const string MapKeyColumn = "MapKey";

        internal static readonly string[] ActionKeys = { ActionMove, ActionScrap };
        internal static readonly string[] ScreenColumns = { "LABEL", "STAT_COLOR", MapKeyColumn };

        internal static string ActionLabel(string action)
        {
            return action ?? string.Empty;
        }

        internal static DataTable PrepareMap(DataTable incoming)
        {
            DataTable prepared = incoming == null ? new DataTable() : incoming.Copy();

            if (!prepared.Columns.Contains(MapKeyColumn))
            {
                prepared.Columns.Add(MapKeyColumn, typeof(string));
            }

            foreach (DataRow row in prepared.Rows)
            {
                row[MapKeyColumn] = MapStableKey(row);
            }

            return prepared;
        }

        internal static int CountFilled(DataTable wafers)
        {
            int count = 0;

            if (wafers == null)
            {
                return count;
            }

            foreach (DataRow row in wafers.Rows)
            {
                if (TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length > 0)
                {
                    count = count + 1;
                }
            }

            return count;
        }

        internal static bool IsSourceCandidate(DataRow row)
        {
            return IsValidCarrier(row)
                    && TableHelper.ParseInt(
                            TableHelper.CellText(row, ServerFields.Durable.UseNumcnt)) > 0;
        }

        internal static bool IsValidCarrier(DataRow row)
        {
            if (row == null)
            {
                return false;
            }

            int capacity;
            int filled;
            bool capacityParsed = int.TryParse(
                    TableHelper.CellText(row, ServerFields.Durable.Capa),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out capacity);
            bool filledParsed = int.TryParse(
                    TableHelper.CellText(row, ServerFields.Durable.UseNumcnt),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out filled);
            if (!capacityParsed
                    || !filledParsed
                    || capacity <= 0
                    || filled < 0
                    || filled > capacity)
            {
                return false;
            }

            string type = TableHelper.CellText(row, ServerFields.Carrier.DurableTyp).Trim();

            if (type == ServerFields.Carrier.Foup)
            {
                return CountIsValid(row, ServerFields.Carrier.UseFoupCount, ServerFields.Carrier.FoupCapa);
            }

            return type == ServerFields.Carrier.Tray
                    && CountIsValid(row, ServerFields.Carrier.UseStubCount, ServerFields.Carrier.StubCapa)
                    && CountIsValid(row, ServerFields.Carrier.UseLccCount, ServerFields.Carrier.LccCapa);
        }

        private static bool CountIsValid(DataRow row, string countColumn, string capacityColumn)
        {
            int capacity;
            int filled;
            bool capacityParsed = int.TryParse(
                    TableHelper.CellText(row, capacityColumn),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out capacity);
            bool filledParsed = int.TryParse(
                    TableHelper.CellText(row, countColumn),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out filled);
            return capacityParsed
                    && filledParsed
                    && capacity > 0
                    && filled >= 0
                    && filled <= capacity;
        }

        internal static DataTable BuildTargetCandidates(DataTable carriers, string sourceId)
        {
            return BuildTargetCandidates(carriers, sourceId, null);
        }

        internal static DataTable BuildTargetCandidates(
                DataTable carriers, string sourceId, DataRow exchangeSource)
        {
            DataTable targets = carriers == null ? new DataTable() : carriers.Clone();

            if (carriers == null)
            {
                return targets;
            }

            foreach (DataRow row in carriers.Select(
                    string.Empty, ServerFields.Durable.UseNumcnt + " ASC"))
            {
                string candidateId = TableHelper.CellText(row, ServerFields.Durable.DurableId);

                if (!IsValidCarrier(row)
                        || string.Equals(candidateId, sourceId, StringComparison.Ordinal))
                {
                    continue;
                }

                bool accepted = exchangeSource == null
                        ? HasAvailableSlot(row)
                        : AcceptsWholeCarrier(row, exchangeSource);

                if (accepted)
                {
                    targets.ImportRow(row);
                }
            }

            return targets;
        }

        internal static bool AcceptsWholeCarrier(DataRow candidate, DataRow source)
        {
            if (!IsCompletelyEmpty(candidate))
            {
                return false;
            }

            return Fits(candidate, source, ServerFields.Carrier.FoupCapa, ServerFields.Carrier.UseFoupCount)
                    && Fits(candidate, source, ServerFields.Carrier.StubCapa, ServerFields.Carrier.UseStubCount)
                    && Fits(candidate, source, ServerFields.Carrier.LccCapa, ServerFields.Carrier.UseLccCount);
        }

        internal static bool IsCompletelyEmpty(DataRow row)
        {
            string status = TableHelper.CellText(row, ServerFields.Durable.WfLoadStatCd.Column).Trim();

            return string.Equals(status, ServerFields.Durable.WfLoadStatCd.Empty, StringComparison.Ordinal)
                    && Count(row, ServerFields.Durable.UseNumcnt) == 0;
        }

        private static bool Fits(DataRow candidate, DataRow source, string capacityColumn, string usedColumn)
        {
            return Count(candidate, capacityColumn) >= Count(source, usedColumn);
        }

        private static int Count(DataRow row, string column)
        {
            int value;

            return int.TryParse(TableHelper.CellText(row, column).Trim(), out value) ? value : 0;
        }

        internal static bool HasAvailableSlot(DataRow row)
        {
            string type = TableHelper.CellText(row, ServerFields.Carrier.DurableTyp).Trim();

            if (type == ServerFields.Carrier.Foup)
            {
                return TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.UseFoupCount))
                        < TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.FoupCapa));
            }

            return type == ServerFields.Carrier.Tray
                    && (TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.UseStubCount))
                            < TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.StubCapa))
                        || TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.UseLccCount))
                            < TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Carrier.LccCapa)));
        }

        internal static bool CanExecute(
                string action,
                string type,
                string sourceId,
                string targetId,
                DataTable source,
                DataTable target,
                DataTable staged)
        {
            return ActionReason(action, type, sourceId, targetId, source, target, staged).Length == 0;
        }

        internal static string ActionReason(
                string action,
                string type,
                string sourceId,
                string targetId,
                DataTable source,
                DataTable target,
                DataTable staged)
        {
            if (action != ActionMove && action != ActionScrap)
            {
                return "This action is not supported.";
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return "Select a source carrier first.";
            }

            if (!MapIsStable(type, source))
            {
                return "Source map is not ready.";
            }

            if (staged == null || CountFilled(staged) == 0)
            {
                return action == ActionScrap
                        ? "Select source wafers to scrap."
                        : "Preview the wafers to move first.";
            }

            if (!StagedRowsAreCurrent(source, staged))
            {
                return "The source map changed. Preview the move again.";
            }

            if (action == ActionScrap)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(targetId))
            {
                return "Select a target carrier first.";
            }

            if (string.Equals(sourceId, targetId, StringComparison.Ordinal))
            {
                return "Source and target carriers must be different.";
            }

            if (!MapIsStable(type, target))
            {
                return "Target map is not ready.";
            }

            if (CountEmptyRows(target, ServerFields.Lot.SubProdTyp.Wafer)
                    + CountEmptyRows(target, ServerFields.Lot.SubProdTyp.Chip)
                    + EmptyLccPositions(target) == 0)
            {
                return "The target carrier has no empty positions.";
            }

            if (!CanPlaceAll(type, target, staged))
            {
                return "The target does not have enough compatible empty positions.";
            }

            return string.Empty;
        }

        internal static string MapStableKey(DataRow row)
        {
            if (row == null)
            {
                return string.Empty;
            }

            string kind = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column).Trim();
            int position = TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Unit.SlotNo));

            if (position <= 0
                    || (kind != ServerFields.Lot.SubProdTyp.Wafer
                            && kind != ServerFields.Lot.SubProdTyp.Chip
                            && kind != ServerFields.Lot.SubProdTyp.Lamella))
            {
                return string.Empty;
            }

            string key = kind + "|" + position.ToString(CultureInfo.InvariantCulture);

            if (kind != ServerFields.Lot.SubProdTyp.Lamella)
            {
                return key;
            }

            string finger = NormalizeFinger(TableHelper.CellText(row, ServerFields.Unit.FingerId));
            return finger.Length == 0 ? string.Empty : key + "|" + finger;
        }

        private static bool MapIsStable(string type, DataTable map)
        {
            if (map == null
                    || (type != ServerFields.Carrier.Foup && type != ServerFields.Carrier.Tray))
            {
                return false;
            }

            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);

            foreach (DataRow row in map.Rows)
            {
                string kind = TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column).Trim();

                if ((type == ServerFields.Carrier.Foup && kind != ServerFields.Lot.SubProdTyp.Wafer)
                        || (type == ServerFields.Carrier.Tray
                                && kind != ServerFields.Lot.SubProdTyp.Chip
                                && kind != ServerFields.Lot.SubProdTyp.Lamella))
                {
                    return false;
                }

                string key = MapStableKey(row);

                if (key.Length == 0 || !keys.Add(key))
                {
                    return false;
                }
            }

            return map.Rows.Count > 0;
        }

        private static bool StagedRowsAreCurrent(DataTable source, DataTable staged)
        {
            Dictionary<string, string> current = FilledRows(source);
            Dictionary<string, string> selected = FilledRows(staged);

            if (selected.Count == 0)
            {
                return false;
            }

            foreach (KeyValuePair<string, string> row in selected)
            {
                string waferId;

                if (!current.TryGetValue(row.Key, out waferId)
                        || !string.Equals(waferId, row.Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            HashSet<string> selectedLccPositions = LccPositions(staged);

            foreach (DataRow row in source.Rows)
            {
                if (TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length == 0)
                {
                    continue;
                }

                string positionKey = LccPositionKey(row);

                if (positionKey.Length > 0
                        && selectedLccPositions.Contains(positionKey)
                        && !selected.ContainsKey(MapStableKey(row)))
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, string> FilledRows(DataTable table)
        {
            Dictionary<string, string> rows = new Dictionary<string, string>(StringComparer.Ordinal);

            if (table == null)
            {
                return rows;
            }

            foreach (DataRow row in table.Rows)
            {
                string waferId = TableHelper.CellText(row, ServerFields.Unit.WfId).Trim();

                if (waferId.Length == 0)
                {
                    continue;
                }

                string key = MapStableKey(row);

                if (key.Length == 0 || rows.ContainsKey(key))
                {
                    rows.Clear();
                    return rows;
                }

                rows.Add(key, waferId);
            }

            return rows;
        }

        private static bool CanPlaceAll(string type, DataTable target, DataTable staged)
        {
            if (type == ServerFields.Carrier.Foup)
            {
                return CountEmptyRows(target, ServerFields.Lot.SubProdTyp.Wafer) >= CountFilled(staged);
            }

            if (type != ServerFields.Carrier.Tray)
            {
                return false;
            }

            int stagedStubs = CountFilledRows(staged, ServerFields.Lot.SubProdTyp.Chip);
            int emptyStubs = CountEmptyRows(target, ServerFields.Lot.SubProdTyp.Chip);
            int stagedLccPositions = LccPositions(staged).Count;
            int emptyLccPositions = EmptyLccPositions(target);

            return stagedStubs <= emptyStubs && stagedLccPositions <= emptyLccPositions;
        }

        internal static string StagingCapacityReason(string type, DataTable target, DataTable staged)
        {
            if (target == null || staged == null || CountFilled(staged) == 0
                    || !MapIsStable(type, target))
            {
                return string.Empty;
            }

            if (type == ServerFields.Carrier.Foup)
            {
                int emptySlots = CountEmptyRows(target, ServerFields.Lot.SubProdTyp.Wafer);
                int plannedSlots = CountFilledRows(staged, ServerFields.Lot.SubProdTyp.Wafer);

                return plannedSlots <= emptySlots
                        ? string.Empty
                        : "Target has only " + emptySlots.ToString("N0")
                                + " compatible empty slots; " + plannedSlots.ToString("N0")
                                + " would be staged.";
            }

            if (type != ServerFields.Carrier.Tray)
            {
                return string.Empty;
            }

            int emptyStubs = CountEmptyRows(target, ServerFields.Lot.SubProdTyp.Chip);
            int plannedStubs = CountFilledRows(staged, ServerFields.Lot.SubProdTyp.Chip);

            if (plannedStubs > emptyStubs)
            {
                return "Target has only " + emptyStubs.ToString("N0")
                        + " empty STUB positions; " + plannedStubs.ToString("N0")
                        + " would be staged.";
            }

            int emptyLccs = EmptyLccPositions(target);
            int plannedLccs = LccPositions(staged).Count;

            return plannedLccs <= emptyLccs
                    ? string.Empty
                    : "Target has only " + emptyLccs.ToString("N0")
                            + " empty LCC positions; " + plannedLccs.ToString("N0")
                            + " would be staged.";
        }

        private static int CountFilledRows(DataTable table, string kind)
        {
            int count = 0;

            foreach (DataRow row in table.Rows)
            {
                if (string.Equals(
                        TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column).Trim(),
                        kind,
                        StringComparison.OrdinalIgnoreCase)
                        && TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEmptyRows(DataTable table, string kind)
        {
            int count = 0;

            foreach (DataRow row in table.Rows)
            {
                if (string.Equals(
                        TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column).Trim(),
                        kind,
                        StringComparison.OrdinalIgnoreCase)
                        && TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length == 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static HashSet<string> LccPositions(DataTable table)
        {
            HashSet<string> positions = new HashSet<string>(StringComparer.Ordinal);

            if (table == null)
            {
                return positions;
            }

            foreach (DataRow row in table.Rows)
            {
                if (TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length == 0)
                {
                    continue;
                }

                string key = LccPositionKey(row);

                if (key.Length > 0)
                {
                    positions.Add(key);
                }
            }

            return positions;
        }

        private static int EmptyLccPositions(DataTable target)
        {
            Dictionary<string, bool> empty = new Dictionary<string, bool>(StringComparer.Ordinal);

            foreach (DataRow row in target.Rows)
            {
                string key = LccPositionKey(row);

                if (key.Length == 0)
                {
                    continue;
                }

                if (!empty.ContainsKey(key))
                {
                    empty[key] = true;
                }

                if (TableHelper.CellText(row, ServerFields.Unit.WfId).Trim().Length > 0)
                {
                    empty[key] = false;
                }
            }

            int count = 0;

            foreach (bool available in empty.Values)
            {
                if (available)
                {
                    count++;
                }
            }

            return count;
        }

        private static string LccPositionKey(DataRow row)
        {
            if (!string.Equals(
                    TableHelper.CellText(row, ServerFields.Lot.SubProdTyp.Column).Trim(),
                    ServerFields.Lot.SubProdTyp.Lamella,
                    StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            int position = TableHelper.ParseInt(TableHelper.CellText(row, ServerFields.Unit.SlotNo));
            return position <= 0
                    ? string.Empty
                    : ServerFields.Lot.SubProdTyp.Lamella + "|" + position.ToString(CultureInfo.InvariantCulture);
        }

        private static string NormalizeFinger(string finger)
        {
            string value = (finger ?? string.Empty).Trim().ToUpperInvariant();

            if (value.Length == 0 || value == "-" || value == "--"
                    || value == "N/A" || value == "NA" || value == "NULL")
            {
                return string.Empty;
            }

            return value;
        }
    }
}
