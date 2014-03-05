using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
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
                using (var runspace = RunspaceFactory.CreateRunspace())
                {
                    log.Trace("Opening runspace");
                    runspace.Open();

                    log.Trace("Creating pipeline");
                    using (var pipeline = runspace.CreatePipeline(command))
                    using (var eventFlag = new ManualResetEvent(false))
                    {
                        pipeline.Input.Close();

                        List<string> output = new List<string>();
                        pipeline.Output.DataReady += (s, e) =>
                            {
                                var data = pipeline.Output.NonBlockingRead();
                                foreach (var obj in data)
                                {
                                    var result = obj.ToString();
                                    log.Trace("Received data from pipeline: {0}", result);
                                    output.Add(result);
                                }
                            };
                        pipeline.Error.DataReady += (s, e) =>
                            {
                                var data = pipeline.Error.NonBlockingRead();
                                foreach (var obj in data)
                                {
                                    var result = obj.ToString();
                                    log.Error("Received error from pipeline: {0}", result);
                                }
                            };

                        pipeline.StateChanged += (s, e) =>
                            {
                                if (pipeline.PipelineStateInfo.State == PipelineState.Completed)
                                {
                                    tcs.TrySetResult(output);
                                    eventFlag.Set();
                                }
                            };

                        log.Trace("Invoking command: {0}", command);
                        pipeline.InvokeAsync();
                        eventFlag.WaitOne();
                    }

                    runspace.Close();
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
                using (var runspace = RunspaceFactory.CreateRunspace())
                {
                    log.Trace("Opening runspace");
                    runspace.Open();

                    log.Trace("Creating pipeline");
                    using (var pipeline = runspace.CreatePipeline(command))
                    using (var eventFlag = new ManualResetEvent(false))
                    {
                        cancelToken.Register(() => pipeline.Stop());

                        pipeline.Input.Close();

                        pipeline.Output.DataReady += (s, e) =>
                            {
                                var data = pipeline.Output.NonBlockingRead();
                                if (data != null)
                                {
                                    foreach (var obj in data)
                                    {
                                        var result = obj.ToString();
                                        log.Trace("Received data from pipeline: {0}", result);
                                        if (onDataReceived != null)
                                            onDataReceived(result);
                                    }
                                }

                                if (pipeline.Output.EndOfPipeline)
                                {
                                    tcs.TrySetResult(null);
                                    eventFlag.Set();
                                }
                            };

                        pipeline.Error.DataReady += (s, e) =>
                            {
                                var data = pipeline.Output.NonBlockingRead();
                                if (data != null)
                                {
                                    foreach (var obj in data)
                                    {
                                        var result = obj.ToString();
                                        log.Trace("Received data from pipeline: {0}", result);
                                        if (onDataReceived != null)
                                            onDataReceived(result);
                                    }
                                }

                                if (pipeline.Error.EndOfPipeline)
                                {
                                    tcs.TrySetResult(null);
                                    eventFlag.Set();
                                }
                            };

                        pipeline.InvokeAsync();
                        eventFlag.WaitOne();
                    }

                    runspace.Close();
                }
            }, cancelToken);

            return tcs.Task;
        }
    }
}
