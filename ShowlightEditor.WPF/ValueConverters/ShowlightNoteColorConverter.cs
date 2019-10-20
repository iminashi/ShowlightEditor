using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ShowlightEditor.WPF.ValueConverters
{
    internal sealed class ShowlightNoteColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int note && note != 0)
            {
                return (Color)Application.Current.TryFindResource("Color" + note.ToString());
            }
            else
            {
                return default(Color);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
