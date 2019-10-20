using System;
using System.Globalization;
using System.Windows.Data;

namespace ShowlightEditor.WPF.ValueConverters
{
    internal sealed class BooleanInversionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool val)
                return !val;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool val)
                return !val;

            return false;
        }
    }
}
