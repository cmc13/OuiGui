using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace OuiGui.WPF.Converters
{
    public class StringToInlineCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var valueString = value as string;
            if (valueString == null)
                return null;

            var regexString = @"\(?https?://[-A-Za-z0-9+&@#/%?=~_()|!:,.;]*[-A-Za-z0-9+&@#/%=~_()|]";
            var regex = new Regex(regexString);
            var tb = new TextBlock { TextWrapping = System.Windows.TextWrapping.Wrap };

            int prevIndex = 0;
            foreach (Match match in regex.Matches(valueString))
            {
                if (match.Index > 0)
                    tb.Inlines.Add(valueString.Substring(prevIndex, match.Index - prevIndex));

                bool p = match.Value.StartsWith("(") && match.Value.EndsWith(")");
                string hyperlink = match.Value;
                if (p)
                {
                    hyperlink = match.Value.Substring(1, match.Value.Length - 2);
                    tb.Inlines.Add("(");
                }

                var h = new Hyperlink(new Run(hyperlink)) { NavigateUri = new Uri(hyperlink) };
                h.RequestNavigate += (s, e) =>
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
                    e.Handled = true;
                };
                tb.Inlines.Add(h);

                if (p) tb.Inlines.Add(")");

                prevIndex = match.Index + match.Length;
            }

            if (prevIndex < valueString.Length)
                tb.Inlines.Add(valueString.Substring(prevIndex));

            return tb;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
