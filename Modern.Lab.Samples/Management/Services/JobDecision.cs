using System.Data;
using Modern.Lab.Data;
using Modern.Lab.Samples.Contracts;
using Modern.Lab.Samples.Services;

namespace Modern.Lab.Samples.Services
{


    public sealed class JobDecision
    {
        public DataRow Equipment { get; set; }

        public DataRow InPort { get; set; }

        public DataRow OutPort { get; set; }

        public DataRow Lot { get; set; }

        public DataRow Durable { get; set; }

        public bool Locked { get; set; }

        public string EqpId
        {
            get { return Cell(this.Equipment, ServerFields.Equipment.EqpId); }
        }

        public string InPortNm
        {
            get
            {
                string chosen = Cell(this.InPort, ServerFields.Port.PortNm);
                return chosen.Length > 0 ? chosen : Cell(this.Equipment, ServerFields.Port.PortNm);
            }
        }

        public string OutPortNm
        {
            get
            {
                string chosen = Cell(this.OutPort, ServerFields.Port.PortNm);
                return chosen.Length > 0 ? chosen : Cell(this.Equipment, ServerFields.Equipment.GoalPortNm);
            }
        }

        public string LotId
        {
            get { return Cell(this.Lot, ServerFields.Lot.LotId); }
        }

        public string DurableId
        {
            get { return Cell(this.Durable, ServerFields.Durable.DurableId); }
        }

        public void DropDetached()
        {
            this.Equipment = Live(this.Equipment);
            this.InPort = Live(this.InPort);
            this.OutPort = Live(this.OutPort);
            this.Lot = Live(this.Lot);
            this.Durable = Live(this.Durable);
        }

        public static bool IsLive(DataRow row)
        {
            return row != null && row.RowState != DataRowState.Detached && row.RowState != DataRowState.Deleted;
        }

        private static DataRow Live(DataRow row)
        {
            return IsLive(row) ? row : null;
        }

        private static string Cell(DataRow row, string column)
        {
            return IsLive(row) ? TableHelper.CellText(row, column).Trim() : string.Empty;
        }
    }
}
