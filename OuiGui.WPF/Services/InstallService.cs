using OuiGui.Lib.Services;
using OuiGui.WPF.Util;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OuiGui.WPF.Services
{
    [Export(typeof(IInstallService))]
    public class InstallService : IInstallService
    {
        public class DataReceivedEventArgs
        {
            public DataReceivedEventArgs(string data)
            {
                this.Data = data;
            }
        
            public string Data { get; private set; }
        }

        private readonly object lockObj = new object();
        private readonly ObservableCollection<InstallAction> installActions = new ObservableCollection<InstallAction>();
        private ReadOnlyObservableCollection<InstallAction> readOnlyInstallActions;
        private readonly IPackageService packageService;
        private readonly SemaphoreSlim syncLock = new SemaphoreSlim(1);
        private InstallAction currentAction;

        [ImportingConstructor]
        public InstallService(IPackageService packageService)
        {
            this.packageService = packageService;
            this.readOnlyInstallActions = new ReadOnlyObservableCollection<InstallAction>(installActions);
        }

        public ReadOnlyObservableCollection<InstallAction> InstallActions
        {
            get { return this.readOnlyInstallActions; }
        }

        public InstallAction CurrentAction
        {
            get { return this.currentAction; }
            private set
            {
                if (this.currentAction != value)
                {
                    this.currentAction = value;
                    this.OnPropertyChanged("CurrentAction");
                }
            }
        }

        private InstallAction Pop()
        {
            if (this.installActions.Any())
            {
                InstallAction action;
                lock (this.lockObj)
                {
                    action = this.installActions.First();
                    this.installActions.RemoveAt(0);
                }

                return action;
            }

            throw new InvalidOperationException();
        }

        public void Remove(InstallAction action)
        {
            lock (this.lockObj)
            {
                this.installActions.Remove(action);
                action.Package.IsInstallPending = false;
            }
        }

        public async void Push(InstallAction action)
        {
            lock (this.lockObj)
            {
                this.installActions.Add(action);
                action.Package.IsInstallPending = true;
                this.OnPendingInstallAdded();
            }

            if (!this.IsRunning)
                await this.Run();
        }

        public void Clear()
        {
            lock (this.lockObj)
            {
                this.installActions.Clear();
            }
        }

        public bool IsRunning
        {
            get { return this.syncLock.CurrentCount <= 0; }
        }

        private async Task Run()
        {
            try
            {
                await this.syncLock.WaitAsync();
                this.OnPropertyChanged("IsRunning");

                while (this.installActions.Any())
                {
                    this.CurrentAction = this.Pop();
                    await this.RunAction(this.CurrentAction);
                    this.CurrentAction.Package.IsInstallPending = false;
                }
            }
            finally
            {
                this.CurrentAction = null;
                this.syncLock.Release();
                this.OnPropertyChanged("IsRunning");
                this.OnInstallCompleted();
            }
        }

        private async Task RunAction(InstallAction action)
        {
            switch (action.Action)
            {
                case InstallActionType.Install:
                    await this.packageService.Install(action.Package, this.OnDataReceived);
                    break;

                case InstallActionType.Uninstall:
                    await this.packageService.Uninstall(action.Package, this.OnDataReceived);
                    break;

                case InstallActionType.Update:
                    await this.packageService.Update(action.Package, this.OnDataReceived);
                    break;
            }
        }

        private void OnDataReceived(string data)
        {
            var handler = this.DataReceived;
            if (handler != null)
                handler(this, new DataReceivedEventArgs(data));
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler InstallCompleted;
        public event EventHandler PendingInstallAdded;
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        protected virtual void OnInstallCompleted()
        {
            var handler = this.InstallCompleted;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected virtual void OnPendingInstallAdded()
        {
            var handler = this.PendingInstallAdded;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
