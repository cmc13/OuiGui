namespace OuiGui.WPF.ViewModels.Messages
{
    public class SearchMessage
    {
        public SearchMessage(string searchText)
        {
            this.SearchText = searchText;
        }

        public string SearchText { get; private set; }
    }
}
