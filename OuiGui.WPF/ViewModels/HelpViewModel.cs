using GalaSoft.MvvmLight;
using System.ComponentModel.Composition;

namespace OuiGui.WPF.ViewModels
{
    [Export]
    public class HelpViewModel : ViewModelBase
    {
        private string markdown = Properties.Resources.Help;

        public string Markdown
        {
            get { return this.markdown; }
            set
            {
                if (this.markdown != value)
                {
                    this.markdown = value;
                    base.RaisePropertyChanged(() => this.Markdown);
                }
            }
        }
    }
}
