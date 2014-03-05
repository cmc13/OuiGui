using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace OuiGui.WPF.Converters
{
    public class StringToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strValue = value as string;
            if (string.IsNullOrWhiteSpace(strValue))
                return DependencyProperty.UnsetValue;

            try
            {
                var image = new BitmapImage();

                image.BeginInit();
                image.UriSource = new Uri(strValue, UriKind.Absolute);
                image.EndInit();

                return image;
            }
            catch (Exception)
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
