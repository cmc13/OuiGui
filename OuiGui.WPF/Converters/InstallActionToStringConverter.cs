using OuiGui.WPF.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OuiGui.WPF.Converters
{
    class InstallActionToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            InstallActionType actionType = (InstallActionType)value;

            switch (actionType)
            {
                case InstallActionType.Install:
                    return "Installing";
                case InstallActionType.Uninstall:
                    return "Uninstalling";
                case InstallActionType.Update:
                    return "Updating";
            }

            throw new ArgumentException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
