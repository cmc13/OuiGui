using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace OuiGui.Lib.Services
{
    [Export(typeof(IPowerShellService))]
    public class PowerShellService : IPowerShellService
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public Task<IEnumerable<string>> RunCommand(CancellationToken cancelToken, string command)
        {
            var tcs = new TaskCompletionSource<IEnumerable<string>>();

            Task.Factory.StartNew(() =>
            {
                using (var ps = PowerShell.Create())
                {
                    var collection = ps.AddScript(command)
                        .Invoke<string>();

                    tcs.TrySetResult(collection);
                }
            });

            return tcs.Task;
        }

        public Task RunCommand(string command, Action<string> onDataReceived)
        {
            return this.RunCommand(command, onDataReceived, CancellationToken.None);
        }

        public Task RunCommand(string command, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            var tcs = new TaskCompletionSource<object>();

            Task.Factory.StartNew(() =>
            {
                using (var ps = PowerShell.Create())
                {
                    var collection = ps.AddScript(command)
                        .Invoke<string>();
                    foreach (var line in collection)
                        onDataReceived(line);

                    tcs.TrySetResult(null);
                }
            }, cancelToken);

            return tcs.Task;
        }
    }
}
