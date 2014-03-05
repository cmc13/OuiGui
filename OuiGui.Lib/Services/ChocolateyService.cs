using NLog;
using OuiGui.Lib.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OuiGui.Lib.Services
{
    [Export(typeof(IChocolateyService))]
    public class ChocolateyService : IChocolateyService
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IPowerShellService powerShellService;

        [ImportingConstructor]
        public ChocolateyService(IPowerShellService powerShellService)
        {
            if (powerShellService == null)
                throw new ArgumentNullException("powerShellService");
            this.powerShellService = powerShellService;
        }

        public async Task<IEnumerable<InstalledPackage>> ListInstalledPackages(bool allVersions, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var regex = new Regex(@"^(.+) ([^ ]+)$");

            IEnumerable<string> psOutput = null;
            try
            {
                string command = "chocolatey list -localonly";
                if (allVersions)
                    command += " -allversions";

                psOutput = await this.powerShellService.RunCommand(token, command);
            }
            catch (TaskCanceledException) { }

            List<InstalledPackage> packages = new List<InstalledPackage>();
            foreach (var str in psOutput.AllButLast())
            {
                token.ThrowIfCancellationRequested();

                var match = regex.Match(str);
                if (match.Success)
                {
                    var ip = new InstalledPackage
                    {
                        Title = match.Groups[1].Value,
                        Version = match.Groups[2].Value
                    };

                    log.Trace("Parsed installed package (Title: {0}, Version: {1})", ip.Title, ip.Version);
                    packages.Add(ip);
                }
                else
                    log.Warn("Failed to parse package: {0}", str);
            }

            if (!packages.Any(p => p.Title.Equals("chocolatey", StringComparison.CurrentCultureIgnoreCase)))
            {
                token.ThrowIfCancellationRequested();

                log.Trace("Trying to get current version of chocolatey package");
                string command = "chocolatey help";
                psOutput = await this.powerShellService.RunCommand(token, command);
                var versionRegex = new Regex(@"^\s*Version\s*:\s*'([^']*)'\s*$");
                foreach (var output in psOutput)
                {
                    var match = versionRegex.Match(output);
                    if (match != null && match.Success)
                    {
                        log.Trace("Parsed chocolatey version: {0}", match.Groups[1].Value);
                        packages.Add(new InstalledPackage
                            {
                                Title = "chocolatey",
                                Version = match.Groups[1].Value
                            });

                        break;
                    }
                }
            }

            return packages;
        }

        public Task InstallPackage(string packageName, string version, Action<string> onDataReceived)
        {
            return this.InstallPackage(packageName, version, onDataReceived, CancellationToken.None);
        }

        public Task UpdatePackage(string packageName, Action<string> onDataReceived)
        {
            return this.UpdatePackage(packageName, onDataReceived, CancellationToken.None);
        }

        public Task UninstallPackage(string packageName, string version, Action<string> onDataReceived)
        {
            return this.UninstallPackage(packageName, version, onDataReceived, CancellationToken.None);
        }

        public async Task InstallPackage(string packageName, string version, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            log.Trace("Installing package {0} (v{1})", packageName, version);

            var builder = new StringBuilder("chocolatey install ")
                .Append(packageName);
            if (!string.IsNullOrWhiteSpace(version))
                builder.Append(" -version ").Append(version);

            await this.powerShellService.RunCommand(builder.ToString(), onDataReceived, cancelToken);
        }

        public async Task UninstallPackage(string packageName, string version, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            log.Trace("Uninstalling package {0} (v{1})", packageName, version);

            var builder = new StringBuilder("chocolatey uninstall ")
                .Append(packageName);
            if (!string.IsNullOrWhiteSpace(version))
                builder.Append(" -version ").Append(version);

            await this.powerShellService.RunCommand(builder.ToString(), onDataReceived, cancelToken);
        }

        public async Task UpdatePackage(string packageName, Action<string> onDataReceived, CancellationToken cancelToken)
        {
            log.Trace("Updating package {0}", packageName);

            string command = string.Format("chocolatey update {0}", packageName);
            await this.powerShellService.RunCommand(command, onDataReceived, cancelToken);
        }
    }
}
