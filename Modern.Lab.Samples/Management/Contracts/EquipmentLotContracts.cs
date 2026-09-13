using Modern.Lab.Samples.Hosting.ResponseContracts;
using Modern.Lab.Samples.Management.Services;

namespace Modern.Lab.Samples.Management.Contracts
{

    public static class EquipmentLotContracts
    {
        public const string EquipmentTable = "Equipment/Lots.GetEqpList.Equipment";
        public const string PortTable = "Equipment/Lots.GetEqpPortList.Port";
        public const string LotTable = "Equipment/Lots.GetJobLotList.Lot";
        public const string DurableTable = "Equipment/Lots.GetJobDurableList.Durable";
        public const string RequestTable = "Equipment/Lots.GetReqList.Request";
        public const string SpecimenTable = "Equipment/Lots.GetRequestInfo.Specimen";

        public const string JobPrep = EquipmentLotPresenter.ActionJobPrep;
        public const string JobStart = EquipmentLotPresenter.ActionJobStart;
        public const string JobEnd = EquipmentLotPresenter.ActionJobEnd;

        public const string AutoDecision = "AutoDecision";

        public const string PortInSlot = "In";
        public const string PortOutSlot = "Out";

        public const string PortInSlotLabel = "In Port";
        public const string PortOutSlotLabel = "Out Port";

        public static string SlotLabel(string slot)
        {
            if (slot == PortInSlot)
            {
                return PortInSlotLabel;
            }

            if (slot == PortOutSlot)
            {
                return PortOutSlotLabel;
            }

            return slot;
        }

        public static ResponseContractSet Build()
        {
            return new ResponseContractSet.Builder(ColumnAliasCatalog.Empty)
                    .Table(Equipment())
                    .Table(Port())
                    .Table(Lot())
                    .Table(Durable())
                    .Table(Request())
                    .Table(Specimen())

                    .Action(
                            JobPrep,
                            EquipmentTable,
                            ActionColumnRequirement.Required(ServerFields.Priority),
                            ActionColumnRequirement.Status(ServerFields.Equipment.CommStatTyp.Column))
                    .Action(
                            JobPrep,
                            PortTable,
                            PortSlots(),
                            ActionColumnRequirement.Required(ServerFields.Priority),
                            ActionColumnRequirement.Status(ServerFields.Port.PortTyp.Column),
                            ActionColumnRequirement.Status(ServerFields.Port.TransferStatCd.Column))
                    .Action(
                            JobPrep,
                            LotTable,
                            ActionColumnRequirement.Required(ServerFields.Priority),
                            ActionColumnRequirement.Status(ServerFields.Lot.LastEventCd.Column),
                            ActionColumnRequirement.Status(ServerFields.Lot.LotHoldStatCd.Column),
                            ActionColumnRequirement.Status(ServerFields.Lot.LotStatTyp.Column),
                            ActionColumnRequirement.Status(ServerFields.Lot.MesProcStatCd.Column))
                    .Action(
                            JobPrep,
                            DurableTable,
                            ActionColumnRequirement.Required(ServerFields.Priority),
                            ActionColumnRequirement.Status(ServerFields.Durable.WfLoadStatCd.Column))

                    .Action(
                            JobStart,
                            EquipmentTable,
                            ActionColumnRequirement.Required(ServerFields.Priority),
                            ActionColumnRequirement.Status(ServerFields.Equipment.CommStatTyp.Column))
                    .Action(
                            JobStart,
                            LotTable,
                            ActionColumnRequirement.Required(ServerFields.Priority),
                            ActionColumnRequirement.Status(ServerFields.Lot.LastEventCd.Column),
                            ActionColumnRequirement.Status(ServerFields.Lot.LotHoldStatCd.Column),
                            ActionColumnRequirement.Required(ServerFields.Lot.EqpId))

                    .Action(
                            JobEnd,
                            EquipmentTable,
                            ActionColumnRequirement.Required(ServerFields.Priority),
                            ActionColumnRequirement.Status(ServerFields.Equipment.CommStatTyp.Column))
                    .Action(
                            JobEnd,
                            LotTable,
                            ActionColumnRequirement.Required(ServerFields.Priority),
                            ActionColumnRequirement.Status(ServerFields.Lot.LastEventCd.Column),
                            ActionColumnRequirement.Status(ServerFields.Lot.LotHoldStatCd.Column),
                            ActionColumnRequirement.Required(ServerFields.Lot.EqpId))

                    .Action(
                            AutoDecision,
                            EquipmentTable,
                            ActionColumnRequirement.Required(ServerFields.Priority))
                    .Action(
                            AutoDecision,
                            PortTable,
                            PortSlots(),
                            ActionColumnRequirement.Required(ServerFields.Priority),
                            ActionColumnRequirement.Status(ServerFields.Port.PortTyp.Column))
                    .Action(
                            AutoDecision,
                            LotTable,
                            ActionColumnRequirement.Required(ServerFields.Priority))
                    .Action(
                            AutoDecision,
                            DurableTable,
                            ActionColumnRequirement.Required(ServerFields.Priority))
                    .Build();
        }

        private static string[] PortSlots()
        {
            return new string[] { PortInSlot, PortOutSlot };
        }

        private static TableContract Equipment()
        {
            return new TableContract(EquipmentTable)
                    .Key(ServerFields.Equipment.EqpId)
                    .Status(ServerFields.Equipment.CommStatTyp.Column, ServerFields.Equipment.CommStatTyp.All)
                    .Status(ServerFields.Equipment.MesEqpStatCd.Column, ServerFields.Equipment.MesEqpStatCd.All)
                    .Status(ServerFields.Equipment.AutoYn.Column, ServerFields.Equipment.AutoYn.All)
                    .Numeric(ServerFields.Priority);
        }

        private static TableContract Port()
        {
            return new TableContract(PortTable)
                    .Key(ServerFields.Port.PortNm)
                    .Status(ServerFields.Port.PortTyp.Column, ServerFields.Port.PortTyp.All)
                    .Status(ServerFields.Port.TransferStatCd.Column, ServerFields.Port.TransferStatCd.All)
                    .Numeric(ServerFields.Priority);
        }

        private static TableContract Lot()
        {
            return new TableContract(LotTable)
                    .Key(ServerFields.Lot.LotId)
                    .Standard(ServerFields.Lot.EqpId)
                    .StatusUnrestricted(ServerFields.Lot.LastEventCd.Column)
                    .Status(ServerFields.Lot.LotHoldStatCd.Column, ServerFields.Lot.LotHoldStatCd.All)
                    .Status(ServerFields.Lot.MesProcStatCd.Column, ServerFields.Lot.MesProcStatCd.All)
                    .Status(ServerFields.Lot.LotStatTyp.Column, ServerFields.Lot.LotStatTyp.All)
                    .Numeric(ServerFields.Priority);
        }

        private static TableContract Durable()
        {
            return new TableContract(DurableTable)
                    .Key(ServerFields.Durable.DurableId)
                    .Status(ServerFields.Durable.WfLoadStatCd.Column, ServerFields.Durable.WfLoadStatCd.All)
                    .Numeric(ServerFields.Durable.UseNumcnt)
                    .Numeric(ServerFields.Priority);
        }

        private static TableContract Request()
        {
            return new TableContract(RequestTable)
                    .Key(ServerFields.Request.ReqSerialNo);
        }

        private static TableContract Specimen()
        {
            return new TableContract(SpecimenTable);
        }
    }
}
