using Modern.Lab.MasterData.Services;

namespace Modern.Lab.MasterData
{
    public partial class PfoNodeForm
    {
        protected override bool UseServerMessaging
        {
            get { return true; }
        }

        protected override string SendRequestText(string request)
        {
            return PfoNodeLocalServer.Send(request);
        }
    }
}
