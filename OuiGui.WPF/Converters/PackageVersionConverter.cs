using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OuiGui.WPF.Converters
{
    public class PackageVersionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string installedVersion = values[0] as string;
            string latestVersion = values[1] as string;

            if (installedVersion != null && !installedVersion.Equals(latestVersion, StringComparison.CurrentCultureIgnoreCase))
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
