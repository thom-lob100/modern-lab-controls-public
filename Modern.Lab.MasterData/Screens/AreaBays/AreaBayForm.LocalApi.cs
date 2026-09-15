using System.Data;

using Modern.Lab.MasterData.Services;

namespace Modern.Lab.MasterData
{
    public partial class AreaBayForm
    {
        protected override bool UseServerMessaging
        {
            get { return true; }
        }

        protected override string SendRequestText(string request)
        {
            return AreaBayLocalServer.Send(request);
        }

        protected override DataTable ParseQueryData(string sendMessage)
        {
            return AreaBayLocalServer.ParseTable(sendMessage);
        }
    }
}
