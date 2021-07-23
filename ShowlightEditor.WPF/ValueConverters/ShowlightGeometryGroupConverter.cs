using Rocksmith2014.XML;

using ShowlightEditor.Core.ViewModels;

using ShowLightGenerator;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ShowlightEditor.WPF.ValueConverters
{
    internal sealed class ShowlightGeometryGroupConverter : IValueConverter
    {
        private static readonly GeometryGroup fogGeoGroup = new GeometryGroup
        {
            Children = new GeometryCollection
            {
                new RectangleGeometry
                {
                    Rect = new Rect(0, 0, 25, 25)
                }
            }
        };

        private static readonly GeometryGroup beamGeoGroup = new GeometryGroup
        {
            Children = new GeometryCollection
            {
                new EllipseGeometry
                {
                    Center = new Point(5, 5),
                    RadiusX = 2,
                    RadiusY = 2
                },
                new EllipseGeometry
                {
                    Center = new Point(5, 16),
                    RadiusX = 4,
                    RadiusY = 4
                },
                new EllipseGeometry
                {
                    Center = new Point(19, 5),
                    RadiusX = 4,
                    RadiusY = 4
                },
                new EllipseGeometry
                {
                    Center = new Point(19, 16),
                    RadiusX = 2,
                    RadiusY = 2
                }
            }
        };

        private static readonly GeometryGroup laserGeoGroup = new GeometryGroup
        {
            Children = new GeometryCollection
            {
                new LineGeometry(new Point(11, 4), new Point(2, 18)),
                new LineGeometry(new Point(12, 5), new Point(12, 18)),
                new LineGeometry(new Point(13, 4), new Point(22, 18)),
                new LineGeometry(new Point(10, 2), new Point(2, 5)),
                new LineGeometry(new Point(14, 2), new Point(22, 5))
            }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte note = (byte)value;
            var showlightType = ShowLightViewModel.GetShowlightType(note);

            if (showlightType == ShowLightType.Fog)
            {
                return fogGeoGroup;
            }

            if (showlightType == ShowLightType.Beam)
            {
                return beamGeoGroup;
            }

            if (note == ShowLight.LasersOff || note == ShowLight.LasersOn)
            {
                return laserGeoGroup;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
