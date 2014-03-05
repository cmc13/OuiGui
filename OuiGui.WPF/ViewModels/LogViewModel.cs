using GalaSoft.MvvmLight;
using NLog;
using OuiGui.WPF.Util;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace OuiGui.WPF.ViewModels
{
    [Export]
    public class LogViewModel : ViewModelBase, IObserver<LogItem>
    {
        private static readonly object objLock = new object();
        private IDisposable unsubscriber;
        private ObservableCollection<LogItem> logItems = new ObservableCollection<LogItem>();

        public LogViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(this.logItems, objLock);

            var target = LogManager.Configuration.AllTargets
                .FirstOrDefault(t => t.GetType() == typeof(MemoryTargetEx))
                as MemoryTargetEx;
            if (target != null)
                target.Subscribe(this);
        }

        public ObservableCollection<LogItem> LogItems
        {
            get { return this.logItems; }
            set
            {
                if (this.logItems != value)
                {
                    this.logItems = value;
                    base.RaisePropertyChanged(() => this.LogItems);
                }
            }
        }

        public void Subscribe(IObservable<LogItem> provider)
        {
            if (provider != null)
                this.unsubscriber = provider.Subscribe(this);
        }

        public void OnCompleted()
        {
            this.unsubscriber.Dispose();
            this.unsubscriber = null;
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(LogItem value)
        {
            this.LogItems.Insert(0, value);
        }
    }
}
