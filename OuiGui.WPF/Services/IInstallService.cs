using OuiGui.WPF.Util;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace OuiGui.WPF.Services
{
    public interface IInstallService : INotifyPropertyChanged
    {
        ReadOnlyObservableCollection<InstallAction> InstallActions { get; }
        InstallAction CurrentAction { get; }
        bool IsRunning { get; }
        void Remove(InstallAction action);
        void Push(InstallAction action);
        void Clear();
        event EventHandler InstallCompleted;
        event EventHandler<OuiGui.WPF.Services.InstallService.DataReceivedEventArgs> DataReceived;
        event EventHandler PendingInstallAdded;
    }
}
