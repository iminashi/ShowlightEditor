using System;
using System.Globalization;
using System.Windows.Data;

namespace ShowlightEditor.WPF.ValueConverters
{
    internal sealed class PluralConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int.TryParse(value.ToString(), out int number);
            if (number == 1)
                return "";

            return "s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
