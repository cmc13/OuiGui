using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;

namespace OuiGui.WPF.Services
{
    [Export(typeof(IDialogService))]
    public class DialogService : IDialogService
    {
        private readonly MetroWindow window;

        public DialogService()
        {
            this.window = Application.Current.MainWindow as MetroWindow;
        }

        public async Task ShowMessageDialog(string title, string message)
        {
            await DialogManager.ShowMessageAsync(this.window, title, message,
                MessageDialogStyle.Affirmative, new MetroDialogSettings
                {
                    AffirmativeButtonText = "Ok"
                });
        }

        public async Task<bool?> ShowYesNoDialog(string title, string message)
        {
            var result = await DialogManager.ShowMessageAsync(this.window, title, message,
                MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                {
                    AffirmativeButtonText = "Yes",
                    NegativeButtonText = "No"
                });
            switch (result)
            {
                case MessageDialogResult.Affirmative:
                    return true;
                case MessageDialogResult.Negative:
                    return false;
                default:
                    return null;
            }
        }
    }
}
