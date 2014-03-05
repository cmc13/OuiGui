using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using OuiGui.Lib.Model;
using OuiGui.Lib.Services;
using OuiGui.WPF.Util;
using OuiGui.WPF.ViewModels.Messages;
using System.Collections.ObjectModel;

namespace OuiGui.WPF.ViewModels
{
    public class PackageDetailsViewModel : ViewModelBase
    {
        #region Private Data Members

        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly Package package;
        private readonly ObservableCollection<PackageVersion> versionHistory = new ObservableCollection<PackageVersion>();
        private readonly IPackageService packageService;
        private readonly IMessenger messenger;
        private bool versionHistoryLoading = false;

        #endregion

        #region Public Constructor Definition

        public PackageDetailsViewModel(Package package, IPackageService packageService, IMessenger messenger)
        {
            this.package = package;
            this.packageService = packageService;
            this.messenger = messenger;

            this.InstallCommand = new RelayCommand(() =>
            {
                log.Trace("Install Command invoked");

                log.Trace("Sending Install request to Install Action ViewModel");
                this.messenger.Send(new InstallAction(this.Package, InstallActionType.Install));
            }, () => !this.package.IsInstalled && !this.package.IsInstallPending);

            this.UpdateCommand = new RelayCommand(() =>
            {
                log.Trace("Update Command invoked");

                log.Trace("Sending update request to Install Action ViewModel");
                this.messenger.Send(new InstallAction(this.Package, InstallActionType.Update));
            }, () => this.package.IsInstalled && !this.package.IsInstallPending && !this.Package.InstalledVersion.Equals(this.Package.Version));

            this.UninstallCommand = new RelayCommand(() =>
            {
                log.Trace("Uninstall Command invoked");

                log.Trace("Sending uninstall request to Install Action ViewModel");
                this.messenger.Send(new InstallAction(this.Package, InstallActionType.Uninstall));
            }, () => !this.package.IsInstallPending && this.package.IsInstalled && !this.package.Title.Equals("chocolatey", System.StringComparison.CurrentCultureIgnoreCase));

            this.InstallVersionCommand = new RelayCommand<PackageVersion>(p =>
            {
                log.Trace("Install Version Command invoked ({0} v{1})", p.Title, p.Version);

                log.Trace("Sending install request to Install Action ViewModel");
                this.messenger.Send(new InstallAction(p, InstallActionType.Install));
            }, p => p != null && !p.IsInstalled && !p.IsInstallPending);

            this.UninstallVersionCommand = new RelayCommand<PackageVersion>(p =>
            {
                log.Trace("Uninstall Version Command invoked ({0} v{1})", p.Title, p.Version);

                log.Trace("Sending uninstall request to Install Action ViewModel");
                this.messenger.Send(new InstallAction(p, InstallActionType.Uninstall));
            }, p => p != null && p.IsInstalled && !p.IsInstallPending && !p.Title.Equals("chocolatey", System.StringComparison.CurrentCultureIgnoreCase));

            this.SearchCommand = new RelayCommand<string>(s =>
            {
                log.Trace("Search Command issued for string: {0}", s);
                this.messenger.Send(new SearchMessage(s));
            }, s => !string.IsNullOrWhiteSpace(s));

            this.LoadVersionHistory();
        }

        #endregion

        #region Command Declarations

        public RelayCommand InstallCommand { get; private set; }

        public RelayCommand UpdateCommand { get; private set; }

        public RelayCommand UninstallCommand { get; private set; }

        public RelayCommand LoadCommand { get; private set; }

        public RelayCommand<PackageVersion> InstallVersionCommand { get; private set; }

        public RelayCommand<PackageVersion> UninstallVersionCommand { get; private set; }

        public RelayCommand<string> SearchCommand { get; private set; }

        #endregion

        #region Public Property Definitions

        public Package Package
        {
            get { return this.package; }
        }

        public ObservableCollection<PackageVersion> VersionHistory
        {
            get { return this.versionHistory; }
        }

        public bool VersionHistoryLoading
        {
            get { return this.versionHistoryLoading; }
            set
            {
                if (this.versionHistoryLoading != value)
                {
                    this.versionHistoryLoading = value;
                    base.RaisePropertyChanged(() => this.VersionHistoryLoading);
                }
            }
        }

        #endregion

        #region Private Function Definitions

        private async void LoadVersionHistory()
        {
            this.VersionHistoryLoading = true;

            log.Trace("Loading version history for package {0}", string.IsNullOrWhiteSpace(this.package.Title) ? "<Untitled Package>" : this.package.Title);
            var versions = await this.packageService.GetVersionHistory(this.package);

            log.Trace("Clearing current version history");
            this.versionHistory.Clear();

            if (versions != null)
            {
                log.Trace("Adding version history to collection");
                foreach (var version in versions)
                    this.versionHistory.Add(version);
            }

            log.Trace("Version history loaded successfully");
            this.VersionHistoryLoading = false;
        }

        #endregion
    }
}
