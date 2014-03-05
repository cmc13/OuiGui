using OuiGui.Lib.ChocolateyPackageService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OuiGui.Lib.Model
{
    public class Package : PackageVersion
    {
        private string installedVersion;

        #region Constructor Definitions

        internal Package(V2FeedPackage feedPackage, InstalledPackage installedPackage)
        {
            this.Id = feedPackage.Id;
            this.Title = string.IsNullOrWhiteSpace(feedPackage.Title) ? feedPackage.Id : feedPackage.Title;
            this.Version = feedPackage.Version;
            this.IsLatestVersion = feedPackage.IsLatestVersion;
            this.IsPrerelease = feedPackage.IsPrerelease;
            this.PackageSize = feedPackage.PackageSize;
            this.IconUrl = feedPackage.IconUrl;
            this.DownloadCount = feedPackage.DownloadCount;
            this.VersionDownloadCount = feedPackage.VersionDownloadCount;
            this.LastUpdated = feedPackage.Published;
            this.GalleryDetailsUrl = feedPackage.GalleryDetailsUrl;
            this.ProjectUrl = feedPackage.ProjectUrl;
            this.LicenseUrl = feedPackage.LicenseUrl;
            this.ReportAbuseUrl = feedPackage.ReportAbuseUrl;
            this.Authors = feedPackage.Authors;
            this.Description = feedPackage.Description;
            this.ReleaseNotes = feedPackage.ReleaseNotes;
            this.Copyright = feedPackage.Copyright;

            if (!string.IsNullOrWhiteSpace(feedPackage.Dependencies))
                this.Dependencies = ParseDependencies(feedPackage.Dependencies);
            else
                this.Dependencies = new string[0];

            if (!string.IsNullOrWhiteSpace(feedPackage.Tags))
                this.Tags = feedPackage.Tags.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            this.IsInstalled = installedPackage != null;
            this.InstalledVersion = installedPackage != null ? installedPackage.Version : null;
        }

        internal Package(V2FeedPackage feedPackage, IEnumerable<InstalledPackage> installedPackages)
            : this(feedPackage, installedPackages
            .Where(p => p.Title.Equals(feedPackage.Id, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(p => p.Version.Equals(feedPackage.Version, StringComparison.CurrentCultureIgnoreCase))
            .LastOrDefault())
        { }

        #endregion

        #region Public Property Definitions
        
        public string InstalledVersion
        {
            get { return this.installedVersion; }
            set
            {
                if (this.installedVersion != value)
                {
                    this.installedVersion = value;
                    base.OnPropertyChanged(() => this.InstalledVersion);
                }
            }
        }
        
        public long PackageSize { get; set; }

        public string IconUrl { get; set; }

        public bool HasIconUrl
        {
            get { return !string.IsNullOrWhiteSpace(this.IconUrl); }
        }

        public int VersionDownloadCount { get; set; }

        public string GalleryDetailsUrl { get; set; }

        public string ProjectUrl { get; set; }

        public string LicenseUrl { get; set; }

        public string ReportAbuseUrl { get; set; }

        public string Authors { get; set; }

        public string Description { get; set; }

        public string ReleaseNotes { get; set; }

        public string Copyright { get; set; }

        public string[] Dependencies { get; set; }

        public string[] Tags { get; set; }

        public bool IsLatestVersion { get; set; }

        #endregion

        #region Private Function Definitions

        private string[] ParseDependencies(string dependencyString)
        {
            List<string> dependencies = new List<string>();
            foreach (var dependency in dependencyString.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var depRegex = new Regex(@"(?<packageName>[a-zA-Z0-9.]+)\s*:(?<versionString>.*):");
                var match = depRegex.Match(dependency);
                var name = match.Groups["packageName"].Value.Trim();
                var version = match.Groups["versionString"].Value.Trim();

                string minVersion = "";
                string maxVersion = "";

                var versionRegex = new Regex(@"[[(]\s*(?<minVersion>[^, ]*)(,\s*(?<maxVersion>[^])]*))?[])]");
                match = versionRegex.Match(version);
                if (match.Success)
                {
                    minVersion = match.Groups["minVersion"].Value;
                    maxVersion = match.Groups["maxVersion"].Value;
                }
                else if (!string.IsNullOrWhiteSpace(match.Value))
                {
                    minVersion = match.Value;
                }

                var builder = new StringBuilder(name);
                if (!string.IsNullOrWhiteSpace(minVersion) || !string.IsNullOrWhiteSpace(maxVersion))
                {
                    builder.Append(" (");

                    if (!string.IsNullOrWhiteSpace(minVersion))
                    {
                        if (version.StartsWith("[") && version.Contains(","))
                            builder.Append('>');
                        if (version.StartsWith("[") || !version.StartsWith("("))
                            builder.Append("= ");
                        builder.Append(minVersion);

                        if (!string.IsNullOrWhiteSpace(maxVersion))
                            builder.Append(" && ");
                    }

                    if (!string.IsNullOrWhiteSpace(maxVersion))
                    {
                        builder.Append('<');
                        if (version.EndsWith("]"))
                            builder.Append("= ");
                        builder.Append(maxVersion);
                    }

                    builder.Append(')');
                }

                dependencies.Add(builder.ToString());
            }

            return dependencies.ToArray();
        }

        #endregion
    }
}
