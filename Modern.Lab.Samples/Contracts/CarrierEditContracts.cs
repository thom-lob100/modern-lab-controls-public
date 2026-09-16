using System;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting.ResponseContracts;
using Modern.Lab.Samples.Services;

namespace Modern.Lab.Samples.Contracts
{
    public static class CarrierEditContracts
    {
        public const string CarrierTable = "CarrierEdit.GetCarriers.Carrier";
        public const string SourceMapTable = "CarrierEdit.GetCarrierWafers.SourceMap";
        public const string TargetMapTable = "CarrierEdit.GetCarrierWafers.TargetMap";

        public const string SourceSlot = "Source";
        public const string TargetSlot = "Target";

        public static ResponseContractSet Build()
        {
            return Build(ServerFields.Carrier.Foup);
        }

        public static ResponseContractSet Build(string type)
        {
            ColumnAliasCatalog aliases = new ColumnAliasCatalog();

            return new ResponseContractSet.Builder(aliases)
                    .Table(Carriers(type))
                    .Table(Map(SourceMapTable, type))
                    .Table(Map(TargetMapTable, type))
                    .Action(
                            CarrierEditPresenter.ActionMove,
                            CarrierTable,
                            new string[] { SourceSlot, TargetSlot },
                            ActionColumnRequirement.Required(ServerFields.Durable.UseNumcnt),
                            ActionColumnRequirement.Required(ServerFields.Durable.Capa))
                    .Action(CarrierEditPresenter.ActionMove, SourceMapTable, MapRequirements(type))
                    .Action(CarrierEditPresenter.ActionMove, TargetMapTable, MapRequirements(type))
                    .Action(
                            CarrierEditPresenter.ActionScrap,
                            CarrierTable,
                            new string[] { SourceSlot },
                            ActionColumnRequirement.Required(ServerFields.Durable.UseNumcnt),
                            ActionColumnRequirement.Required(ServerFields.Durable.Capa))
                    .Action(CarrierEditPresenter.ActionScrap, SourceMapTable, MapRequirements(type))
                    .Build();
        }

        private static TableContract Carriers(string type)
        {
            TableContract contract = new TableContract(CarrierTable)
                    .Key(ServerFields.Durable.DurableId)
                    .Standard(ServerFields.Carrier.DurableTyp)
                    .RequireNumeric(ServerFields.Durable.UseNumcnt, ServerFields.Durable.Capa);

            if (string.Equals(type, ServerFields.Carrier.Tray, StringComparison.Ordinal))
            {
                return contract.RequireNumeric(
                        ServerFields.Carrier.UseStubCount,
                        ServerFields.Carrier.StubCapa,
                        ServerFields.Carrier.UseLccCount,
                        ServerFields.Carrier.LccCapa);
            }

            return contract.RequireNumeric(
                    ServerFields.Carrier.UseFoupCount, ServerFields.Carrier.FoupCapa);
        }

        private static TableContract Map(string tableId, string type)
        {
            TableContract contract = new TableContract(tableId)
                    .Key(CarrierEditPresenter.MapKeyColumn)
                    .Numeric(ServerFields.Unit.SlotNo)
                    .Standard(
                            ServerFields.Lot.SubProdTyp.Column,
                            ServerFields.Unit.WfId,
                            ServerFields.Lot.LotId);

            if (string.Equals(type, ServerFields.Carrier.Tray, StringComparison.Ordinal))
            {
                return contract.Standard(
                        ServerFields.Unit.FingerId, ServerFields.Unit.FingerIndex);
            }

            return contract;
        }

        private static ActionColumnRequirement[] MapRequirements(string type)
        {
            if (string.Equals(type, ServerFields.Carrier.Tray, StringComparison.Ordinal))
            {
                return new ActionColumnRequirement[]
                {
                    ActionColumnRequirement.Required(ServerFields.Lot.SubProdTyp.Column),
                    ActionColumnRequirement.Required(ServerFields.Unit.SlotNo),
                    ActionColumnRequirement.Present(ServerFields.Unit.FingerId),
                    ActionColumnRequirement.Present(ServerFields.Unit.FingerIndex),
                    ActionColumnRequirement.Present(ServerFields.Unit.WfId),
                    ActionColumnRequirement.Present(ServerFields.Lot.LotId)
                };
            }

            return new ActionColumnRequirement[]
            {
                ActionColumnRequirement.Required(ServerFields.Lot.SubProdTyp.Column),
                ActionColumnRequirement.Required(ServerFields.Unit.SlotNo),
                ActionColumnRequirement.Present(ServerFields.Unit.WfId),
                ActionColumnRequirement.Present(ServerFields.Lot.LotId)
            };
        }
    }
}
