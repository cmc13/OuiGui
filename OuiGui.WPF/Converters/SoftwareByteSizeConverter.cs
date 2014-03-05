using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OuiGui.WPF.Converters
{
    public class SoftwareByteSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string[] sizes = new string[] { "B", "kB", "MB", "GB" };

            var sizeInBytes = System.Convert.ToDouble((long)value);
            int order = 0;
            while (sizeInBytes > 1024 && order + 1 < sizes.Length)
            {
                order++;
                sizeInBytes /= 1024;
            }

            return string.Format("{0:0.##} {1}", sizeInBytes, sizes[order]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
