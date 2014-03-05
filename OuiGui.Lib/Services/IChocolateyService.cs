using OuiGui.Lib.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OuiGui.Lib.Services
{
    public interface IChocolateyService
    {
        Task<IEnumerable<InstalledPackage>> ListInstalledPackages(bool allVersions, CancellationToken token);
        Task InstallPackage(string packageName, string version, Action<string> onDataReceived);
        Task UninstallPackage(string packageName, string version, Action<string> onDataReceived);
        Task UpdatePackage(string packageName, Action<string> onDataReceived);
        Task InstallPackage(string packageName, string version, Action<string> onDataReceived, CancellationToken cancelToken);
        Task UninstallPackage(string packageName, string version, Action<string> onDataReceived, CancellationToken cancelToken);
        Task UpdatePackage(string packageName, Action<string> onDataReceived, CancellationToken cancelToken);
    }
}
