namespace OuiGui.WPF.ViewModels.Messages
{
    public class RefreshMessage
    {
        private static readonly RefreshMessage instance = new RefreshMessage();

        private RefreshMessage() { }

        public static RefreshMessage Instance
        {
            get { return instance; }
        }
    }
}
