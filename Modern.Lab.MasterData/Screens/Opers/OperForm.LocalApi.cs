using Modern.Lab.MasterData.Services;

namespace Modern.Lab.MasterData
{
    public partial class OperForm
    {
        protected override bool UseServerMessaging
        {
            get { return true; }
        }

        protected override string SendRequestText(string request)
        {
            return OperLocalServer.Send(request);
        }
    }
}
