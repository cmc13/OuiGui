using System.ComponentModel;

namespace OuiGui.Lib
{
    public enum PackageFilter
    {
        [Description("Stable Only")]
        StableOnly,

        [Description("Include Prerelease")]
        IncludePrerelease,

        [Description("Installed Only")]
        InstalledOnly
    }
}
