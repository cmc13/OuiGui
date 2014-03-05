using NLog;
using OuiGui.Lib.ChocolateyPackageService;
using OuiGui.Lib.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OuiGui.Lib.Services
{
    [Export(typeof(IPackageService))]
    public class PackageService : IPackageService
    {
        #region Private Data Members

        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly FeedContext_x0060_1 context;
        private readonly IChocolateyService chocolateyService;
        private string cachedSearchText = null;
        private ObservableCollection<Package> cachedSearchResults = null;
        private ObservableCollection<Package> cachedPrereleasePackages = null;
        private Task searchTask = null;
        private Task listPrereleaseTask = null;

        #endregion

        #region Public Constructor Definition

        [ImportingConstructor]
        public PackageService(FeedContext_x0060_1 context, IChocolateyService chocolateyService)
        {
            this.context = context;
            this.chocolateyService = chocolateyService;
        }

        #endregion

        #region Public Function Definitions

        public Task<IEnumerable<Package>> ListAvailablePackages(int skip, int take, bool includePrerelease,
            Action<int> totalItemCountCallback, CancellationToken token)
        {
            if (this.searchTask != null)
            {
                this.cachedSearchResults.Clear();
                this.searchTask.Dispose();
                this.searchTask = null;
            }

            if (includePrerelease)
                return this.ListAvailablePrereleasePackages(skip, take, totalItemCountCallback, token);
            else
            {
                if (this.listPrereleaseTask != null)
                {
                    this.cachedPrereleasePackages.Clear();
                    this.listPrereleaseTask.Dispose();
                    this.listPrereleaseTask = null;
                }

                return this.ListAvailablePackages(skip, take, totalItemCountCallback, token);
            }
        }

        public Task<IEnumerable<Package>> SearchAvailablePackages(int skip, int take, bool includePrerelease, string searchText,
            Action<int> totalItemCountCallback, CancellationToken token)
        {
            if (this.listPrereleaseTask != null)
            {
                this.cachedPrereleasePackages.Clear();
                this.listPrereleaseTask = null;
            }

            var tcs = new TaskCompletionSource<IEnumerable<Package>>();
            NotifyCollectionChangedEventHandler handler = (s, e) =>
            {
                var packageList = s as ObservableCollection<Package>;
                if (token.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    if (packageList.Count >= (skip + take))
                        tcs.TrySetResult(packageList.OrderBy(p => p.Title).Skip(skip).Take(take));
                }
            };

            if (searchText.Equals(this.cachedSearchText, StringComparison.CurrentCultureIgnoreCase)
                && this.searchTask != null)
            {
                var packageList = this.cachedSearchResults;
                if (this.searchTask.IsCompleted)
                    tcs.TrySetResult(packageList.Skip(skip).Take(take));
                else
                {
                    packageList.CollectionChanged += handler;
                    this.searchTask.ContinueWith(t => tcs.TrySetResult(packageList.Skip(skip).Take(take)), token);
                }
            }
            else
            {
                var installedPackagesTask = this.chocolateyService.ListInstalledPackages(false, token);

                this.cachedSearchText = searchText;
                var packageList = new ObservableCollection<Package>();
                this.cachedSearchResults = packageList;
                packageList.CollectionChanged += handler;

                this.searchTask = Task.Factory.StartNew(() =>
                    {
                        var query = (DataServiceQuery<V2FeedPackage>)this.context.Packages
                            .Where(p => p.IsLatestVersion || (includePrerelease && p.IsPrerelease));

                        QueryOperationResponse<V2FeedPackage> result;
                        try
                        {
                            result = query.Execute() as QueryOperationResponse<V2FeedPackage>;
                        }
                        catch (Exception ex)
                        {
                            if (token.IsCancellationRequested)
                                tcs.TrySetCanceled();
                            else if (ex is InvalidOperationException
                                && ex.InnerException != null
                                && ex.InnerException is DataServiceClientException
                                && ex.InnerException.InnerException != null
                                && ex.InnerException.InnerException is System.Net.WebException)
                            {
                                tcs.TrySetResult(Enumerable.Empty<Package>());
                            }
                            else
                            {
                                tcs.TrySetException(ex);
                            }

                            return;
                        }

                        while (true)
                        {
                            if (token.IsCancellationRequested)
                            {
                                tcs.TrySetCanceled();
                                return;
                            }

                            var list = result.ToList();

                            foreach (var package in list
                                .Where(p =>
                                {
                                    if (p.Title != null && p.Title.ToLower().IndexOf(searchText) >= 0)
                                        return true;

                                    if (p.Authors != null && p.Authors.ToLower().IndexOf(searchText) >= 0)
                                        return true;

                                    if (p.Tags != null && p.Tags.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Any(t => t.Equals(searchText, StringComparison.CurrentCultureIgnoreCase)))
                                        return true;

                                    return false;
                                }))
                            {
                                packageList.Add(new Package(package, installedPackagesTask.Result));
                            }

                            if (token.IsCancellationRequested)
                            {
                                tcs.TrySetCanceled();
                                return;
                            }

                            totalItemCountCallback(packageList.Count);

                            var continuation = result.GetContinuation();
                            if (continuation == null)
                                break;

                            try
                            {
                                result = this.context.Execute(continuation);
                            }
                            catch (Exception ex)
                            {
                                if (ex is InvalidOperationException
                                    && ex.InnerException != null
                                    && ex.InnerException is DataServiceClientException
                                    && ex.InnerException.InnerException != null
                                    && ex.InnerException.InnerException is System.Net.WebException)
                                {
                                    tcs.TrySetResult(Enumerable.Empty<Package>());
                                }
                                else
                                {
                                    tcs.TrySetException(ex);
                                }

                                return;
                            }
                        }

                        packageList.CollectionChanged -= handler;
                        tcs.TrySetResult(packageList.OrderBy(p => p.Title).Skip(skip).Take(take));
                    }, token);
            };

            return tcs.Task;
        }

        public async Task<IEnumerable<Package>> ListInstalledPackages(int skip, int take, Action<int> totalItemCountCallback, CancellationToken token)
        {
            if (this.searchTask != null)
            {
                this.cachedSearchResults.Clear();
                this.searchTask.Dispose();
                this.searchTask = null;
            }

            if (this.listPrereleaseTask != null)
            {
                this.cachedPrereleasePackages.Clear();
                this.listPrereleaseTask.Dispose();
                this.listPrereleaseTask = null;
            }

            var installedPackages = await this.chocolateyService.ListInstalledPackages(false, token);

            List<Package> packages = new List<Package>();
            foreach (var installedPackage in installedPackages)
            {
                token.ThrowIfCancellationRequested();
                packages.Add(await this.GetPackage(installedPackage, token));
            }

            token.ThrowIfCancellationRequested();

            totalItemCountCallback(packages.Count);

            return packages.Skip(skip).Take(take);
        }

        public async Task<IEnumerable<Package>> SearchInstalledPackages(int skip, int take, string searchText,
            Action<int> totalItemCountCallback, CancellationToken token)
        {
            if (this.searchTask != null)
            {
                this.cachedSearchResults.Clear();
                this.searchTask.Dispose();
                this.searchTask = null;
            }

            if (this.listPrereleaseTask != null)
            {
                this.cachedPrereleasePackages.Clear();
                this.listPrereleaseTask.Dispose();
                this.listPrereleaseTask = null;
            }

            var installedPackages = await this.chocolateyService.ListInstalledPackages(false, token);

            List<Package> packages = new List<Package>();
            foreach (var installedPackage in installedPackages)
            {
                token.ThrowIfCancellationRequested();
                var package = await this.GetPackage(installedPackage, token);
                if ((package.Title != null && package.Title.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    || (package.Authors != null && package.Authors.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    || (package.Tags != null && package.Tags.Any(t => t.Equals(searchText, StringComparison.CurrentCultureIgnoreCase))))
                {
                    packages.Add(package);
                }
            }

            totalItemCountCallback(packages.Count);

            return packages;
        }

        public Task<IEnumerable<PackageVersion>> GetVersionHistory(Package package)
        {
            var tcs = new TaskCompletionSource<IEnumerable<PackageVersion>>();
            var query = (DataServiceQuery<V2FeedPackage>)this.context.Packages
                .Where(p => p.Id.ToLower().Equals(package.Id.ToLower()));
            var installedPackagesTask = this.chocolateyService.ListInstalledPackages(true, CancellationToken.None);

            query.BeginExecute(r =>
                {
                    QueryOperationResponse<V2FeedPackage> result;
                    try
                    {
                        result = query.EndExecute(r) as QueryOperationResponse<V2FeedPackage>;
                    }
                    catch (Exception ex)
                    {
                        if (ex is InvalidOperationException
                            && ex.InnerException != null
                            && ex.InnerException is DataServiceClientException
                            && ex.InnerException.InnerException != null
                            && ex.InnerException.InnerException is System.Net.WebException)
                        {
                            tcs.TrySetResult(Enumerable.Empty<PackageVersion>());
                        }
                        else
                        {
                            tcs.TrySetException(ex);
                        }

                        return;
                    }

                    List<PackageVersion> packages = new List<PackageVersion>();
                    while (true)
                    {
                        var list = result.ToList();

                        packages.AddRange(list.Select(p => new PackageVersion
                            {
                                Id = p.Id,
                                Title = p.Title,
                                DownloadCount = p.VersionDownloadCount,
                                Version = p.Version,
                                LastUpdated = p.Published,
                                IsInstalled = installedPackagesTask.Result.Any(ip => ip.Title.Equals(p.Id, StringComparison.CurrentCultureIgnoreCase)
                                    && ip.Version.Equals(p.Version, StringComparison.CurrentCultureIgnoreCase)),
                                IsPrerelease = p.IsPrerelease
                            }));

                        var continuation = result.GetContinuation();
                        if (continuation == null)
                            break;

                        try
                        {
                            result = this.context.Execute(continuation);
                        }
                        catch (Exception ex)
                        {
                            if (ex is InvalidOperationException
                                && ex.InnerException != null
                                && ex.InnerException is DataServiceClientException
                                && ex.InnerException.InnerException != null
                                && ex.InnerException.InnerException is System.Net.WebException)
                            {
                                tcs.TrySetResult(Enumerable.Empty<PackageVersion>());
                            }
                            else
                            {
                                tcs.TrySetException(ex);
                            }

                            return;
                        }
                    }

                    tcs.TrySetResult(packages.OrderByDescending(p => p.LastUpdated));
                }, null);

            return tcs.Task;
        }

        public Task Install(Package package, Action<string> onDataReceived)
        {
            return this.Install(package, onDataReceived, CancellationToken.None);
        }

        public Task Uninstall(Package package, Action<string> onDataReceived)
        {
            return this.Uninstall(package, onDataReceived, CancellationToken.None);
        }

        public Task Install(PackageVersion package, Action<string> onDataReceived)
        {
            return this.Install(package, onDataReceived, CancellationToken.None);
        }

        public Task Uninstall(PackageVersion package, Action<string> onDataReceived)
        {
            return this.Uninstall(package, onDataReceived, CancellationToken.None);
        }

        public Task Update(PackageVersion package, Action<string> onDataReceived)
        {
            return this.Update(package, onDataReceived, CancellationToken.None);
        }

        public Task Install(Package package, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            return this.Install(package.Id, package.Version, onDataReceived, cancelToken);
        }

        public Task Uninstall(Package package, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            return this.Uninstall(package.Id, package.Version, onDataReceived, cancelToken);
        }

        public Task Install(PackageVersion package, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            return this.Install(package.Id, package.Version, onDataReceived, cancelToken);
        }

        public Task Uninstall(PackageVersion package, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            return this.Uninstall(package.Id, package.Version, onDataReceived, cancelToken);
        }

        public Task Update(PackageVersion package, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            return this.Update(package.Id, onDataReceived, cancelToken);
        }

        public async Task Install(string id, string version, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            await this.chocolateyService.InstallPackage(id, version, onDataReceived, cancelToken);
        }

        public async Task Uninstall(string id, string version, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            await this.chocolateyService.UninstallPackage(id, version, onDataReceived, cancelToken);
        }

        public async Task Update(string id, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            await this.chocolateyService.UpdatePackage(id, onDataReceived, cancelToken);
        }

        public Task Install(PackageVersion package)
        {
            return this.Install(package, null);
        }

        public Task Uninstall(PackageVersion package)
        {
            return this.Uninstall(package, null);
        }

        public Task Update(PackageVersion package)
        {
            return this.Update(package, null);
        }

        #endregion

        #region Private Function Definitions

        private Task<Package> GetPackage(InstalledPackage installedPackage, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<Package>();

            var query = (DataServiceQuery<V2FeedPackage>)this.context.Packages
                .Where(p => p.Id.ToLower().Equals(installedPackage.Title)
                    && (p.IsLatestVersion
                        || (p.IsPrerelease && p.Version.ToLower().Equals(installedPackage.Version))))
                .OrderBy(p => p.IsLatestVersion).ThenBy(p => p.Published)
                .Take(1);

            if (token.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
                return tcs.Task;
            }

            query.BeginExecute(r =>
                {
                    if (token.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                        return;
                    }

                    QueryOperationResponse<V2FeedPackage> result;
                    try
                    {
                        result = query.EndExecute(r) as QueryOperationResponse<V2FeedPackage>;
                    }
                    catch (Exception ex)
                    {
                        if (ex is InvalidOperationException
                            && ex.InnerException != null
                            && ex.InnerException is DataServiceClientException
                            && ex.InnerException.InnerException != null
                            && ex.InnerException.InnerException is System.Net.WebException)
                        {
                            tcs.TrySetResult(null);
                        }
                        else
                        {
                            tcs.TrySetException(ex);
                        }

                        return;
                    }

                    tcs.SetResult(new Package(result.Single(), installedPackage));
                }, null);

            return tcs.Task;
        }

        private Task<IEnumerable<Package>> ListAvailablePackages(int skip, int take, Action<int> totalItemCountCallback, CancellationToken token)
        {
            this.searchTask = null;
            var tcs = new TaskCompletionSource<IEnumerable<Package>>();

            var query = (DataServiceQuery<V2FeedPackage>)this.context.Packages
                .IncludeTotalCount()
                .Where(p => p.IsLatestVersion)
                .OrderByDescending(p => p.DownloadCount)
                .Skip(skip)
                .Take(take);

            var installedPackagesTask = this.chocolateyService.ListInstalledPackages(false, token);

            log.Trace("Executing query ({0})", query.ToString());
            query.BeginExecute(r =>
            {
                if (token.IsCancellationRequested)
                {
                    log.Trace("List available packages task was cancelled");
                    tcs.TrySetCanceled();
                    return;
                }

                QueryOperationResponse<V2FeedPackage> result;
                try
                {
                    result = query.EndExecute(r) as QueryOperationResponse<V2FeedPackage>;
                }
                catch (Exception ex)
                {
                    if (ex is InvalidOperationException
                        && ex.InnerException != null
                        && ex.InnerException is DataServiceClientException
                        && ex.InnerException.InnerException != null
                        && ex.InnerException.InnerException is System.Net.WebException)
                    {
                        tcs.TrySetResult(Enumerable.Empty<Package>());
                    }
                    else
                    {
                        tcs.TrySetException(ex);
                    }

                    return;
                }

                if (token.IsCancellationRequested)
                {
                    log.Trace("List available packages task was cancelled");
                    tcs.TrySetCanceled();
                    return;
                }

                totalItemCountCallback((int)result.TotalCount);

                List<Package> packages = new List<Package>();
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        log.Trace("List available packages task was cancelled");
                        tcs.TrySetCanceled();
                        return;
                    }

                    var list = result.ToList();
                    try
                    {
                        var installedPackages = installedPackagesTask.Result;
                        foreach (var package in list)
                            packages.Add(new Package(package, installedPackages));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                        return;
                    }

                    var continuation = result.GetContinuation();
                    if (continuation == null)
                        break;

                    try
                    {
                        log.Trace("Executing continuation query ({0})", continuation.NextLinkUri.ToString());
                        result = this.context.Execute(continuation);
                    }
                    catch (Exception ex)
                    {
                        if (ex is InvalidOperationException
                            && ex.InnerException != null
                            && ex.InnerException is DataServiceClientException
                            && ex.InnerException.InnerException != null
                            && ex.InnerException.InnerException is System.Net.WebException)
                        {
                            tcs.TrySetResult(Enumerable.Empty<Package>());
                        }
                        else
                        {
                            tcs.TrySetException(ex);
                        }

                        return;
                    }
                }

                tcs.SetResult(packages);
            }, null);

            return tcs.Task;
        }

        private Task<IEnumerable<Package>> ListAvailablePrereleasePackages(int skip, int take,
            Action<int> totalItemCountCallback, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<IEnumerable<Package>>();
            NotifyCollectionChangedEventHandler handler = null;
            handler = (s, e) =>
            {
                var packageList = s as ObservableCollection<Package>;
                if (token.IsCancellationRequested)
                {
                    this.listPrereleaseTask.Dispose();
                    this.listPrereleaseTask = null;
                    tcs.TrySetCanceled();
                }
                else
                {
                    if (packageList.Count >= skip + take)
                    {
                        packageList.CollectionChanged -= handler;
                        tcs.TrySetResult(packageList.ToList().Skip(skip).Take(take));
                    }
                }
            };

            if (this.listPrereleaseTask != null)
            {
                var packageList = this.cachedPrereleasePackages;
                if (this.listPrereleaseTask.IsCompleted)
                {
                    if (totalItemCountCallback != null)
                        totalItemCountCallback(packageList.Count);
                    packageList.CollectionChanged -= handler;
                    tcs.TrySetResult(packageList.Skip(skip).Take(take));
                }
                else
                {
                    packageList.CollectionChanged += handler;
                    this.listPrereleaseTask.ContinueWith(t => tcs.TrySetResult(packageList.Skip(skip).Take(take)), token);
                }
            }
            else
            {
                var installedPackagesTask = this.chocolateyService.ListInstalledPackages(false, token);
                var packageList = new ObservableCollection<Package>();
                this.cachedPrereleasePackages = packageList;
                packageList.CollectionChanged += handler;

                this.listPrereleaseTask = Task.Factory.StartNew(() =>
                {
                    var query = (DataServiceQuery<V2FeedPackage>)this.context.Packages
                        .Where(p => p.IsLatestVersion || p.IsPrerelease)
                        .OrderBy(p => p.Id).ThenByDescending(p => p.Published);

                    QueryOperationResponse<V2FeedPackage> result;
                    try
                    {
                        result = query.Execute() as QueryOperationResponse<V2FeedPackage>;
                    }
                    catch (Exception ex)
                    {
                        if (token.IsCancellationRequested)
                            tcs.TrySetCanceled();
                        else if (ex is InvalidOperationException
                            && ex.InnerException != null
                            && ex.InnerException is DataServiceClientException
                            && ex.InnerException.InnerException != null
                            && ex.InnerException.InnerException is System.Net.WebException)
                        {
                            tcs.TrySetResult(Enumerable.Empty<Package>());
                        }
                        else
                        {
                            tcs.TrySetException(ex);
                        }

                        this.listPrereleaseTask = null;
                        packageList.CollectionChanged -= handler;
                        return;
                    }

                    while (true)
                    {
                        if (token.IsCancellationRequested)
                        {
                            this.listPrereleaseTask = null;
                            packageList.CollectionChanged -= handler;
                            tcs.TrySetCanceled();
                            return;
                        }

                        var list = result.ToList();

                        foreach (var package in list)
                        {
                            if (!packageList.Any(p => p.Id.Equals(package.Id)))
                                packageList.Add(new Package(package, installedPackagesTask.Result));
                        }

                        if (token.IsCancellationRequested)
                        {
                            this.listPrereleaseTask = null;
                            packageList.CollectionChanged -= handler;
                            tcs.TrySetCanceled();
                            return;
                        }

                        if (totalItemCountCallback != null)
                            totalItemCountCallback(packageList.Count);

                        var continuation = result.GetContinuation();
                        if (continuation == null)
                            break;

                        try
                        {
                            result = this.context.Execute(continuation);
                        }
                        catch (Exception ex)
                        {
                            if (token.IsCancellationRequested)
                                tcs.TrySetCanceled();
                            if (ex is InvalidOperationException
                                && ex.InnerException != null
                                && ex.InnerException is DataServiceClientException
                                && ex.InnerException.InnerException != null
                                && ex.InnerException.InnerException is System.Net.WebException)
                            {
                                tcs.TrySetResult(Enumerable.Empty<Package>());
                            }
                            else
                            {
                                tcs.TrySetException(ex);
                            }

                            packageList.CollectionChanged -= handler;
                            this.listPrereleaseTask = null;
                            return;
                        }
                    }

                    packageList.CollectionChanged -= handler;
                    tcs.TrySetResult(packageList.OrderBy(p => p.Title).Skip(skip).Take(take));
                }, token);
            }

            return tcs.Task;
        }

        #endregion
    }
}
