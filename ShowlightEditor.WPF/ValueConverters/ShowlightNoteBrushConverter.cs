using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ShowlightEditor.WPF.ValueConverters
{
    internal sealed class ShowlightNoteBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Brush)Application.Current.TryFindResource("SolidBrush" + value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
