using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OuiGui.Lib.Services
{
    public interface IPowerShellService
    {
        Task<IEnumerable<string>> RunCommand(CancellationToken cancelToken, string command);
        Task RunCommand(string command, Action<string> onDataReceived);
        Task RunCommand(string command, Action<string> onDataReceived, CancellationToken cancelToken);
    }
}
