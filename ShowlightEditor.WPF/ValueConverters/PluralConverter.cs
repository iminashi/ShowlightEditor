using System;
using System.Globalization;
using System.Windows.Data;

namespace ShowlightEditor.WPF.ValueConverters
{
    internal sealed class PluralConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value.ToString();
            if (int.TryParse(strValue, out int number))
            {
                if (number == 1)
                    return string.Empty;

                return strValue[strValue.Length - 1] == 's' ? "es" : "s";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
