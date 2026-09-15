using Modern.Lab.MasterData.Services;

namespace Modern.Lab.MasterData
{
    public partial class ProductForm
    {
        protected override bool UseServerMessaging
        {
            get { return true; }
        }

        protected override string SendRequestText(string request)
        {
            return ProductLocalServer.Send(request);
        }
    }
}
