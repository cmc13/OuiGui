using OuiGui.Lib.ChocolateyPackageService;
using System.ComponentModel.Composition;
namespace OuiGui.Lib
{
    public class FeedContextAdapter
    {
        private readonly FeedContext_x0060_1 context;

        [ImportingConstructor]
        public FeedContextAdapter([Import("CHOCOLATEY_SERVICE_URL")] string chocolateyServiceUri)
        {
            this.context = new FeedContext_x0060_1(new System.Uri(chocolateyServiceUri));
        }

        [Export]
        public FeedContext_x0060_1 Context
        {
            get { return this.context; }
        }
    }
}
