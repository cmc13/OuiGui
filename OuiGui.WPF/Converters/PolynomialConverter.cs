using System;
using System.Windows.Data;
using System.Windows.Media;

namespace OuiGui.WPF.Converters
{
    public class PolynomialConverter : IValueConverter
    {
        public DoubleCollection Coefficients { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                int x = (int)value;
                double output = 0;
                for (int i = 0; i < Coefficients.Count; ++i)
                    output += Coefficients[i] * Math.Pow(x, (Coefficients.Count - 1) - i);
                return System.Convert.ToInt32(output);
            }

            return int.MinValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
