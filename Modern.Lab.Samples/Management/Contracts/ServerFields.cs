namespace Modern.Lab.Samples.Contracts
{








    public static class ServerFields
    {


        public const string Priority = "PRIORITY";


        public static class Equipment
        {
            public const string EqpId = "EQP_ID";
            public const string EqpNm = "EQP_NM";
            public const string GoalPortNm = "GOAL_PORT_NM";
            public const string ParentEqpId = "PARENT_EQP_ID";
            public const string Location = "LOCATION";


            public static class CommStatTyp
            {
                public const string Column = "COMM_STAT_TYP";

                public const string OnLineRemote = "OnLineRemote";
                public const string OnLineLocal = "OnLineLocal";
                public const string OffLine = "OffLine";

                public static readonly string[] All = { OnLineRemote, OnLineLocal, OffLine };
            }


            public static class MesEqpStatCd
            {
                public const string Column = "MES_EQP_STAT_CD";

                public const string Run = "Run";
                public const string Idle = "Idle";
                public const string Up = "Up";
                public const string Down = "Down";

                public static readonly string[] All = { Run, Idle, Up, Down };
            }


            public static class AutoYn
            {
                public const string Column = "AUTO_YN";

                public const string Y = "Y";
                public const string N = "N";

                public static readonly string[] All = { Y, N };
            }
        }


        public static class Port
        {
            public const string PortNm = "PORT_NM";

            public const string Mode = "MODE";
            public const string DurableType = "DURABLE_TYPE";
            public const string LotId = "LOT_ID";
            public const string DurableId = "DURABLE_ID";


            public static class PortTyp
            {
                public const string Column = "PORT_TYP";

                public const string Input = "Input";
                public const string Output = "Output";
                public const string InputOutput = "InputOutput";

                public static readonly string[] All = { Input, Output, InputOutput };
            }





            public static class TransferStatCd
            {
                public const string Column = "TRANSFER_STAT_CD";

                public const string ReadyToLoad = "ReadyToLoad";
                public const string ReadyToUnload = "ReadyToUnload";
                public const string Processing = "Processing";
                public const string ReservedToLoad = "ReservedToLoad";
                public const string ReservedToUnload = "ReservedToUnload";

                public static readonly string[] All =
                {
                    ReadyToLoad, ReadyToUnload, Processing, ReservedToLoad, ReservedToUnload
                };
            }
        }


        public static class Carrier
        {
            public const string DurableTyp = Durable.DurableTyp;
            public const string FoupCapa = "FOUP_CAPA";
            public const string UseFoupCount = "USE_FOUPCNT";
            public const string UseStubCount = "USE_STUBCNT";
            public const string UseLccCount = "USE_LCCCNT";
            public const string StubCapa = "STUB_CAPA";
            public const string LccCapa = "LCC_CAPA";

            public const string Foup = "FOUP";
            public const string Tray = "TRAY";
        }


        public static class Lot
        {
            public const string LotId = "LOT_ID";

            public static class SubProdTyp
            {
                public const string Column = "SUB_PROD_TYP";

                public const string Wafer = "Wafer";
                public const string Chip = "Chip";
                public const string Lamella = "Lamella";

                public static readonly string[] All = { Wafer, Chip, Lamella };
            }
            public const string Qty = "QTY";
            public const string CreateTm = "CREATE_TM";
            public const string ReqSerialNo = "REQ_SERIAL_NO";

            public const string EqpId = Equipment.EqpId;
            public const string PortNm = Port.PortNm;
            public const string GoalPortNm = "GOAL_PORT_NM";


            public const string CarrierId = "CARRIER_ID";


            public const string GoalCarrierId = "GOAL_CARRIER_ID";




            public static class LastEventCd
            {
                public const string Column = "LAST_EVENT_CD";

                public const string JobPrep = "JobPrep";
                public const string JobStart = "JobStart";
                public const string JobEnd = "JobEnd";
            }


            public static class LotHoldStatCd
            {
                public const string Column = "LOT_HOLD_STAT_CD";

                public const string OnHold = "OnHold";
                public const string NotOnHold = "NotOnHold";

                public static readonly string[] All = { OnHold, NotOnHold };
            }


            public static class LotStatTyp
            {
                public const string Column = "LOT_STAT_TYP";

                public const string Created = "Created";
                public const string Released = "Released";
                public const string Scrapped = "Scrapped";
                public const string Emptied = "Emptied";

                public static readonly string[] All = { Created, Released, Scrapped, Emptied };
            }


            public static class MesProcStatCd
            {
                public const string Column = "MES_PROC_STAT_CD";

                public const string WAIT = "WAIT";
                public const string PROC = "PROC";
                public const string SELE = "SELE";
                public const string RESV = "RESV";
                public const string LOAD = "LOAD";

                public static readonly string[] All = { WAIT, PROC, SELE, RESV, LOAD };
            }
        }



        public static class Durable
        {
            public const string DurableId = "DURABLE_ID";
            public const string DurableType = "DURABLE_TYPE";
            public const string DurableTyp = "DURABLE_TYP";
            public const string FrFabId = "FR_FAB_ID";

            public const string DurableStatCd = "DURABLE_STAT_CD";


            public static class WfLoadStatCd
            {
                public const string Column = "WF_LOAD_STAT_CD";

                public const string Empty = "Empty";
                public const string Partial = "Partial";
                public const string Full = "Full";

                public static readonly string[] All = { Empty, Partial, Full };
            }

            public const string UseNumcnt = "USE_NUMCNT";
            public const string Capa = "CAPA";
            public const string Location = "LOCATION";
            public const string JobLotId = "JOB_LOT_ID";
        }



        public static class Request
        {
            public const string ReqSerialNo = Lot.ReqSerialNo;
            public const string LayerNm = "LAYER_NM";
            public const string Requester = "REQUESTER";
            public const string Department = "DEPARTMENT";
            public const string DeviceNm = "DEVICE_NM";
            public const string ProjectNm = "PROJECT_NM";
            public const string Details = "DETAILS";
        }


        public static class Unit
        {
            public const string WfId = "WF_ID";
            public const string SlotNo = "SLOT_NO";
            public const string FingerId = "FINGER_ID";
            public const string FingerIndex = "FINGER_INDEX";
            public const string ReqSubNo = "REQ_SUB_NO";
        }


        public static class Group
        {
            public const string EqpGrpId = "EQP_GRP_ID";
        }
    }
}
