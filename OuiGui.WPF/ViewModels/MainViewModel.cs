using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using OuiGui.WPF.Services;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OuiGui.WPF.ViewModels
{
    [Export]
    public class MainViewModel : ViewModelBase
    {
        #region Private Data Members

        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private bool showInstallQueue = false;
        private readonly IMessenger messenger;
        private bool showLog = false;
        private readonly IDialogService dialogService;
        private readonly IInstallService installService;
        private bool isBusy;
        private string busyStatus;
        private bool showHelp = false;
        private readonly IConfigurationService configurationService;

        #endregion

        #region Public Constructor Definition

        [ImportingConstructor]
        public MainViewModel(IMessenger messenger, IDialogService dialogService, IInstallService installService,
            IConfigurationService configurationService)
        {
            this.messenger = messenger;
            this.dialogService = dialogService;
            this.installService = installService;
            this.configurationService = configurationService;

            this.installService.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName.Equals("IsRunning"))
                    {
                        if (this.installService.IsRunning)
                            this.ShowInstallQueue = true;
                        else
                            this.ShowInstallQueue = false;
                    }
                };

            this.installService.InstallCompleted += (s, e) => this.RaisePropertyChanged(() => this.PendingInstallCount);
            this.installService.PendingInstallAdded += (s, e) => this.RaisePropertyChanged(() => this.PendingInstallCount);

            this.ShowLogCommand = new RelayCommand(() => this.ShowLog = true);

            this.ShowInstallQueueCommand = new RelayCommand(() => this.ShowInstallQueue = !this.ShowInstallQueue);

            this.ShowHelpCommand = new RelayCommand(() => this.ShowHelp = !this.ShowHelp);

            this.ClosingCommand = new RelayCommand<CancelEventArgs>(async e =>
            {
                if (this.installService.IsRunning)
                {
                    e.Cancel = true;

                    if (!this.IsBusy)
                    {
                        var window = System.Windows.Application.Current.MainWindow as MahApps.Metro.Controls.MetroWindow;
                        var result = await this.dialogService.ShowYesNoDialog("Exit Application?",
                            "There are installs pending, are you sure you want to exit the application?");
                        if (result.HasValue && result.Value)
                        {
                            try
                            {
                                this.BusyStatus = "Waiting for current install to finish";
                                this.IsBusy = true;

                                this.ShowInstallQueue = false;
                                this.installService.Clear();
                                await this.WaitForCurrentInstallToFinish();
                            }
                            finally
                            {
                                this.IsBusy = false;
                                Application.Current.Shutdown();
                            }
                        }
                    }
                }
            });

            this.LoadCommand = new RelayCommand(() =>
            {
                var showHelp = bool.Parse(this.configurationService.GetAppSetting("SHOW_HELP"));
                if (showHelp)
                {
                    this.ShowHelp = showHelp;
                    this.configurationService.SetAppSetting("SHOW_HELP", bool.FalseString);
                    this.configurationService.Save();
                }
            });
        }

        #endregion

        #region Command Definitions

        public RelayCommand ShowLogCommand { get; private set; }
        public RelayCommand ShowInstallQueueCommand { get; private set; }
        public RelayCommand ShowHelpCommand { get; private set; }
        public RelayCommand<CancelEventArgs> ClosingCommand { get; private set; }
        public RelayCommand LoadCommand { get; private set; }

        #endregion

        #region Public Property Definitions

        public bool ShowHelp
        {
            get { return this.showHelp; }
            set
            {
                if (this.showHelp != value)
                {
                    this.showHelp = value;
                    base.RaisePropertyChanged(() => this.ShowHelp);
                }
            }
        }

        public int PendingInstallCount
        {
            get { return this.installService.InstallActions.Count
                + (this.installService.IsRunning ? 1 : 0); }
        }

        public bool ShowInstallQueue
        {
            get { return this.showInstallQueue; }
            set
            {
                if (this.showInstallQueue != value)
                {
                    this.showInstallQueue = value;
                    base.RaisePropertyChanged(() => this.ShowInstallQueue);
                }
            }
        }

        public bool ShowLog
        {
            get { return this.showLog; }
            set
            {
                if (this.showLog != value)
                {
                    this.showLog = value;
                    base.RaisePropertyChanged(() => this.ShowLog);
                }
            }
        }

        public bool IsBusy
        {
            get { return this.isBusy; }
            set
            {
                if (this.isBusy != value)
                {
                    this.isBusy = value;
                    base.RaisePropertyChanged(() => this.IsBusy);
                }
            }
        }

        public string BusyStatus
        {
            get { return this.busyStatus; }
            set
            {
                if (this.busyStatus != value)
                {
                    this.busyStatus = value;
                    base.RaisePropertyChanged(() => this.BusyStatus);
                }
            }
        }

        #endregion

        private Task WaitForCurrentInstallToFinish()
        {
            var tcs = new TaskCompletionSource<object>();
            
            this.installService.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName.Equals("IsRunning") && !this.installService.IsRunning)
                        tcs.TrySetResult(null);
                };

            if (!this.installService.IsRunning)
                tcs.TrySetResult(null);

            return tcs.Task;
        }
    }
}