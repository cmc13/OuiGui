using GalaSoft.MvvmLight.Messaging;
using NLog;
using OuiGui.Lib.Model;
using OuiGui.Lib.Services;
using OuiGui.WPF.ViewModels;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Windows;

namespace OuiGui.WPF.Util
{
    public class ViewModelLocator : IDisposable
    {
        #region Private Data Members

        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static readonly ViewModelLocator current = new ViewModelLocator();
        private CompositionContainer container;
        private AggregateCatalog catalog;

        #endregion

        #region Constructor Definition

        protected ViewModelLocator()
        {
            if ((bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue))
            {
                this.PackageListViewModel = new PackageListViewModel(null, GalaSoft.MvvmLight.Messaging.Messenger.Default);
            }
            else
            {
                this.catalog = new AggregateCatalog();
                this.container = new CompositionContainer(catalog);

                this.catalog.Catalogs.Add(new AssemblyCatalog(this.GetType().Assembly));
                this.catalog.Catalogs.Add(new AssemblyCatalog(typeof(IPackageService).Assembly));

                this.container.ComposeParts(this);
                log.Trace("Composing ViewModelLocator");
            }

            Application.Current.DispatcherUnhandledException += (s, e) =>
                {
                    log.FatalException("Unhandled exception thrown during program execution", e.Exception);
                };
        }

        #endregion

        #region Singleton Instance Property

        public static ViewModelLocator Current
        {
            get { return current; }
        }

        #endregion

        #region Public Property Definitions

        [Export("CHOCOLATEY_SERVICE_URL")]
        public string ChocolateyServiceUrl
        {
            get { return ConfigurationManager.AppSettings["CHOCOLATEY_SERVICE_URL"]; }
        }

        [Export]
        public IMessenger Messenger
        {
            get { return GalaSoft.MvvmLight.Messaging.Messenger.Default; }
        }

        [Import]
        public MainViewModel MainViewModel { get; set; }

        [Import]
        public PackageListViewModel PackageListViewModel { get; set; }

        [Import]
        public LogViewModel LogViewModel { get; set; }

        [Import]
        public InstallActionViewModel InstallActionViewModel { get; set; }

        [Import]
        public HelpViewModel HelpViewModel { get; set; }

        #endregion

        #region Public Function Definitions

        public PackageDetailsViewModel GetPackageDetailsViewModel(Package pkg)
        {
            log.Trace("Composing Package Details ViewModel");
            var packageService = this.container.GetExportedValue<IPackageService>();
            return new PackageDetailsViewModel(pkg, packageService, this.Messenger);
        }

        #endregion

        #region IDisposable Implementation

        ~ViewModelLocator()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.catalog != null)
                {
                    this.catalog.Dispose();
                    this.catalog = null;
                }

                if (this.container != null)
                {
                    this.container.Dispose();
                    this.container = null;
                }
            }
        }

        #endregion
    }
}
