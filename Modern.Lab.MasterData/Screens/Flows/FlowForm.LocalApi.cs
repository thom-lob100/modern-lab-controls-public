using Modern.Lab.MasterData.Services;

namespace Modern.Lab.MasterData
{
    public partial class FlowForm
    {
        protected override bool UseServerMessaging
        {
            get { return true; }
        }

        protected override string SendRequestText(string request)
        {
            return FlowLocalServer.Send(request);
        }
    }
}
