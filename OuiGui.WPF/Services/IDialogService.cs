using System;
namespace OuiGui.WPF.Services
{
    public interface IDialogService
    {
        System.Threading.Tasks.Task ShowMessageDialog(string title, string message);
        System.Threading.Tasks.Task<bool?> ShowYesNoDialog(string title, string message);
    }
}
