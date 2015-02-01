using OuiGui.Lib.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OuiGui.Lib.Services
{
    public interface IPackageService
    {
        Task<Tuple<IEnumerable<Package>, int>> ListAvailablePackages(int skip, int take, bool includePrerelease, CancellationToken token);
        Task<Tuple<IEnumerable<Package>, int>> ListInstalledPackages(int skip, int take, CancellationToken token);
        Task<Tuple<IEnumerable<Package>, int>> SearchAvailablePackages(int skip, int take, bool includePrerelease, string searchText, CancellationToken token);
        Task<Tuple<IEnumerable<Package>, int>> SearchInstalledPackages(int skip, int take, string searchText, CancellationToken token);
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
