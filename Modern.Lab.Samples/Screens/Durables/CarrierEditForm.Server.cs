using System;
using System.Collections.Generic;
using System.Data;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Data;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Samples.Contracts;

namespace Modern.Lab.Samples
{


    public partial class CarrierEditForm
    {
        private DataTable RequestDurables(string type)
        {
            return this.RequestFields(
                    "GetDurableList", "DURABLE_TYPE", type ?? string.Empty).Table;
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
    }
}
