using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OuiGui.WPF.Util
{
    [Target("MemoryTargetEx")]
    public class MemoryTargetEx : Target, IObservable<LogItem>
    {
        private class Unsubscriber : IDisposable
        {
            private List<IObserver<LogItem>> observers;
            private IObserver<LogItem> observer;

            public Unsubscriber(List<IObserver<LogItem>> observers, IObserver<LogItem> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                if (observer != null && observers.Contains(observer))
                    observers.Remove(observer);
            }
        }

        private List<IObserver<LogItem>> observers = new List<IObserver<LogItem>>();

        protected override void Write(LogEventInfo logEvent)
        {
            foreach (var observer in this.observers)
            {
                observer.OnNext(new LogItem
                    {
                        Message = logEvent.FormattedMessage,
                        Logger = logEvent.LoggerName,
                        Timestamp = logEvent.TimeStamp,
                        Severity = logEvent.Level
                    });
            }
        }

        public IDisposable Subscribe(IObserver<LogItem> observer)
        {
            if (!this.observers.Contains(observer))
                this.observers.Add(observer);
            return new Unsubscriber(this.observers, observer);
        }
    }
}
