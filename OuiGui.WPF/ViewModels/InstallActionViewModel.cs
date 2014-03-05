using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OuiGui.Lib.Services;
using OuiGui.WPF.Services;
using OuiGui.WPF.Util;
using OuiGui.WPF.ViewModels.Messages;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

namespace OuiGui.WPF.ViewModels
{
    [Export]
    public class InstallActionViewModel : ViewModelBase
    {
        #region Private Data Members

        private readonly IInstallService installService;
        private ObservableCollection<string> actionLog = new ObservableCollection<string>();
        private readonly IPackageService packageService;
        private readonly IMessenger messenger;

        #endregion

        #region Public Constructor Definition

        [ImportingConstructor]
        public InstallActionViewModel(IPackageService packageService, IMessenger messenger, IInstallService installService)
        {
            this.packageService = packageService;
            this.messenger = messenger;
            this.installService = installService;

            this.installService.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName.Equals("CurrentAction"))
                    {
                        this.RaisePropertyChanged(() => this.CurrentAction);
                    }
                };

            this.installService.InstallCompleted += (s, e) =>
                {
                    this.ActionLog.Clear();
                    this.messenger.Send(RefreshMessage.Instance);
                };

            this.installService.DataReceived += (s, e) => this.ActionLog.Add(e.Data);

            this.CancelCommand = new RelayCommand<InstallAction>(p => this.installService.Remove(p));

            this.messenger.Register<InstallAction>(this, i =>
                {
                    if ((this.CurrentAction == null || this.CurrentAction.Package != i.Package)
                        && !this.InstallActionQueue.Any(a => a.Package.Equals(i.Package)))
                    {
                        this.installService.Push(i);
                    }
                });
        }

        #endregion

        #region Command Definitions

        public RelayCommand<InstallAction> CancelCommand { get; private set; }

        #endregion

        #region Public Property Definitions

        public ReadOnlyObservableCollection<InstallAction> InstallActionQueue
        {
            get { return this.installService.InstallActions; }
        }

        public InstallAction CurrentAction
        {
            get { return this.installService.CurrentAction; }
        }

        public ObservableCollection<string> ActionLog
        {
            get { return this.actionLog; }
            set
            {
                if (this.actionLog != value)
                {
                    this.actionLog = value;
                    base.RaisePropertyChanged(() => this.ActionLog);
                }
            }
        }

        #endregion
    }
}