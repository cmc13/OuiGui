using OuiGui.Lib.Model;
using OuiGui.WPF.Util;
using System;
using System.Windows.Data;

namespace OuiGui.WPF.Converters
{
    public class PackageToPackageDetailsViewModelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var package = value as Package;
            if (package != null)
                return ViewModelLocator.Current.GetPackageDetailsViewModel(package);

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
