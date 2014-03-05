using System;
using System.Windows.Data;

namespace OuiGui.WPF.Converters
{
    public class CountToDigitsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int number = (int)value;
            return number.ToString().Length;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
