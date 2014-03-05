using OuiGui.Lib.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OuiGui.Lib.Services
{
    public interface IPackageService
    {
        Task<IEnumerable<Package>> ListAvailablePackages(int skip, int take, bool includePrerelease, Action<int> totalItemCountCallback, CancellationToken token);
        Task<IEnumerable<Package>> ListInstalledPackages(int skip, int take, Action<int> totalItemCountCallback, CancellationToken token);
        Task<IEnumerable<Package>> SearchAvailablePackages(int skip, int take, bool includePrerelease, string searchText, Action<int> totalItemCountCallback, CancellationToken token);
        Task<IEnumerable<Package>> SearchInstalledPackages(int skip, int take, string searchText, Action<int> totalItemCountCallback, CancellationToken token);
        Task<IEnumerable<PackageVersion>> GetVersionHistory(Package package);

        Task Install(PackageVersion package);
        Task Uninstall(PackageVersion package);
        Task Install(Package package, Action<string> onDataReceived);
        Task Uninstall(Package package, Action<string> onDataReceived);
        Task Install(PackageVersion package, Action<string> onDataReceived);
        Task Uninstall(PackageVersion package, Action<string> onDataReceived);
        Task Update(PackageVersion package, Action<string> onDataReceived);
        Task Update(PackageVersion package);
        Task Install(Package package, Action<string> onDataReceived, CancellationToken cancelToken);
        Task Uninstall(Package package, Action<string> onDataReceived, CancellationToken cancelToken);
        Task Install(PackageVersion package, Action<string> onDataReceived, CancellationToken cancelToken);
        Task Uninstall(PackageVersion package, Action<string> onDataReceived, CancellationToken cancelToken);
        Task Update(PackageVersion package, Action<string> onDataReceived, CancellationToken cancelToken);
        Task Install(string id, string version, Action<string> onDataReceived, CancellationToken cancelToken);
        Task Uninstall(string id, string version, Action<string> onDataReceived, CancellationToken cancelToken);
        Task Update(string id, Action<string> onDataReceived, CancellationToken cancelToken);
    }
}
