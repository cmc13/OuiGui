using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using OuiGui.Lib;
using OuiGui.Lib.Model;
using OuiGui.Lib.Services;
using OuiGui.WPF.ViewModels.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OuiGui.WPF.ViewModels
{
    [Export]
    public class PackageListViewModel : ViewModelBase
    {
        #region Private Data Members

        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static readonly int ITEMS_PER_PAGE = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["PACKAGE_ITEMS_PER_PAGE"])
            ? int.Parse(ConfigurationManager.AppSettings["PACKAGE_ITEMS_PER_PAGE"]) : 40;

        private readonly SemaphoreSlim syncLock = new SemaphoreSlim(1);
        private ObservableCollection<Package> packageList = new ObservableCollection<Package>();
        private readonly IPackageService packageService;
        private bool isBusy = false;
        private string busyStatus = "";
        private int currentPage = 1;
        private int totalItemCount = 0;
        private string searchText;
        private PackageFilter currentFilter = PackageFilter.StableOnly;
        private readonly object lockObj = new object();
        private CancellationTokenSource loadCancelToken = null;
        private readonly IMessenger messenger;

        #endregion

        #region Constructor Definition

        [ImportingConstructor]
        public PackageListViewModel(IPackageService packageService, IMessenger messenger)
        {
            this.packageService = packageService;
            this.messenger = messenger;

            this.GoToPageCommand = new RelayCommand<int>(i =>
                {
                    log.Trace("Go To Page Command invoked");
                    this.CurrentPage = i;
                }, i => i >= 1 && i <= this.PageCount && i != this.CurrentPage);

            this.LoadCommand = new RelayCommand(() =>
                {
                    log.Trace("Load Command invoked");
                    this.Load();
                });

            this.DismissBusyIndicatorCommand = new RelayCommand(() =>
                {
                    log.Trace("Dismiss Busy Indicator Command invoked");
                    this.IsBusy = false;
                }, () => this.IsBusy);

            this.ClearSearchTextCommand = new RelayCommand(() =>
                {
                    log.Trace("Clear Search Text Command invoked");
                    this.SearchText = "";
                });

            this.messenger.Register<RefreshMessage>(this, m =>
                {
                    log.Trace("Received Refresh message");
                    this.Load();
                });

            this.messenger.Register<SearchMessage>(this, s =>
                {
                    log.Trace("Received Search message");
                    this.SearchText = s.SearchText;
                });
        }

        #endregion

        #region Public Command Declarations

        public ICommand LoadCommand { get; private set; }

        public RelayCommand<int> GoToPageCommand { get; private set; }

        public RelayCommand DismissBusyIndicatorCommand { get; private set; }

        public RelayCommand ClearSearchTextCommand { get; private set; }

        #endregion

        #region Public Property Definitions

        public bool IsBusy
        {
            get { return this.isBusy; }
            set
            {
                if (this.isBusy != value)
                {
                    this.isBusy = value;
                    base.RaisePropertyChanged(() => this.IsBusy);
                    this.DismissBusyIndicatorCommand.RaiseCanExecuteChanged();
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

        public ObservableCollection<Package> PackageList
        {
            get { return this.packageList; }
            set
            {
                if (this.packageList != value)
                {
                    this.packageList = value;
                    base.RaisePropertyChanged(() => this.PackageList);
                }
            }
        }

        public int StartIndex
        {
            get { return (this.CurrentPage - 1) * ITEMS_PER_PAGE + 1; }
        }

        public int EndIndex
        {
            get { return Math.Min(this.CurrentPage * ITEMS_PER_PAGE, this.TotalItemCount); }
        }

        public int PageCount
        {
            get { return Math.Max(this.TotalItemCount / ITEMS_PER_PAGE + (this.TotalItemCount % ITEMS_PER_PAGE == 0 ? 0 : 1), 1); }
        }

        public int CurrentPage
        {
            get { return this.currentPage; }
            set
            {
                if (value < 1) value = 1;
                if (value > this.PageCount) value = this.PageCount;

                if (this.currentPage != value)
                {
                    log.Trace("Current page changed from {0} to {1}", this.currentPage, value);
                    this.currentPage = value;
                    base.RaisePropertyChanged(() => this.CurrentPage);
                    base.RaisePropertyChanged(() => this.StartIndex);
                    base.RaisePropertyChanged(() => this.EndIndex);
                    this.Load();
                }
            }
        }

        public int TotalItemCount
        {
            get { return this.totalItemCount; }
            set
            {
                if (this.totalItemCount != value)
                {
                    log.Trace("Total Item Count changed from {0} to {1}", this.totalItemCount, value);
                    this.totalItemCount = value;
                    base.RaisePropertyChanged(() => this.TotalItemCount);
                    base.RaisePropertyChanged(() => this.PageCount);
                    base.RaisePropertyChanged(() => this.EndIndex);
                    this.GoToPageCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SearchText
        {
            get { return this.searchText; }
            set
            {
                if (this.searchText != value)
                {
                    log.Trace("Search Text changed from {0} to {1}", this.searchText, value);
                    this.searchText = value;
                    base.RaisePropertyChanged(() => this.SearchText);

                    if (this.CurrentPage != 1)
                        this.CurrentPage = 1;
                    else
                        this.Load();
                }
            }
        }

        public PackageFilter CurrentFilter
        {
            get { return this.currentFilter; }
            set
            {
                if (this.currentFilter != value)
                {
                    log.Trace("Current Package Filter changed from {0} to {1}", this.currentFilter, value);
                    this.currentFilter = value;
                    base.RaisePropertyChanged(() => this.CurrentFilter);

                    if (this.CurrentPage != 1)
                        this.CurrentPage = 1;
                    else
                        this.Load();
                }
            }
        }

        #endregion

        #region Private Function Definitions

        private async void Load()
        {
            CancellationTokenSource localCancelToken = null;

            try
            {
                log.Trace("Loading current page of package list");

                localCancelToken = new CancellationTokenSource();

                lock (this.lockObj)
                {
                    if (this.loadCancelToken != null)
                    {
                        log.Trace("Cancelling previous load task");
                        this.loadCancelToken.Cancel();
                    }

                    this.loadCancelToken = localCancelToken;
                }

                log.Trace("Waiting for sync lock");
                await syncLock.WaitAsync();

                this.BusyStatus = "Loading Packages";
                this.IsBusy = true;
                await this.LoadCurrentPage(localCancelToken);

                log.Trace("Packages loaded successfully");
            }
            catch (AggregateException agEx)
            {
                if (agEx.InnerExceptions.Count == 1 && (agEx.InnerExceptions.Single() is OperationCanceledException))
                    log.Trace("Previous load task was cancelled");
                else
                    throw;
            }
            catch (OperationCanceledException)
            {
                log.Trace("Previous load task was cancelled");
            }
            finally
            {
                this.IsBusy = false;

                lock (this.lockObj)
                {
                    if (this.loadCancelToken == localCancelToken)
                        this.loadCancelToken = null;
                }

                if (localCancelToken != null)
                    localCancelToken.Dispose();

                log.Trace("Releasing sync lock");
                this.syncLock.Release();
            }
        }

        private async Task LoadCurrentPage(CancellationTokenSource cancelToken)
        {
            if (cancelToken.IsCancellationRequested)
                return;

            int skip = ITEMS_PER_PAGE * (this.CurrentPage - 1);
            int take = ITEMS_PER_PAGE;

            string searchText = null;
            if (this.SearchText != null)
                searchText = this.SearchText.Trim().ToLower();
            IEnumerable<Package> packages = null;
            if (this.packageService != null)
            {
                log.Trace("Requesting packages from package service");
                if (this.CurrentFilter == PackageFilter.InstalledOnly)
                {
                    log.Trace("Filtering on installed packages");

                    if (!string.IsNullOrWhiteSpace(searchText))
                        packages = await this.packageService.SearchInstalledPackages(skip, take, searchText, t => this.TotalItemCount = t, cancelToken.Token);
                    else
                        packages = await this.packageService.ListInstalledPackages(skip, take, t => this.TotalItemCount = t, cancelToken.Token);
                }
                else
                {
                    bool includePrerelease = (this.CurrentFilter == PackageFilter.IncludePrerelease);
                    if (!string.IsNullOrWhiteSpace(searchText))
                        packages = await this.packageService.SearchAvailablePackages(skip, take, includePrerelease, searchText,
                            t => this.TotalItemCount = t, cancelToken.Token);
                    else
                        packages = await this.packageService.ListAvailablePackages(skip, take, includePrerelease,
                            t => this.TotalItemCount = t, cancelToken.Token);
                }
            }

            log.Trace("Clearing old packages from collection");
            this.PackageList.Clear();
            if (packages != null)
            {
                log.Trace("Adding received packages to collection");
                foreach (var pkg in packages)
                    this.PackageList.Add(pkg);
            }
        }

        #endregion
    }
}
