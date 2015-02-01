using OuiGui.Lib.Model;
using OuiGui.WPF.Util;
using System;
using System.Text;
using System.Windows.Data;

namespace OuiGui.WPF.Converters
{
    public class PackageToInstallActionVersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var installAction = value as InstallAction;
            if (installAction != null)
            {
                var builder = new StringBuilder();
                builder.Append('(');
                if (installAction.Action == InstallActionType.Update)
                {
                    var package = installAction.Package as Package;
                    if (package != null)
                    {
                        builder.Append('v')
                            .Append(package.InstalledVersion)
                            .Append(" => ");
                    }
                }

                builder.Append('v')
                    .Append(installAction.Package.Version)
                    .Append(')');

                return builder.ToString();
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
