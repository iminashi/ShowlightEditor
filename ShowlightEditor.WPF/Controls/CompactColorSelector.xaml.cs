using ShowlightEditor.Core;
using ShowlightEditor.Core.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ShowlightEditor.WPF.Controls
{
    /// <summary>
    /// Interaction logic for CompactColorSelector.xaml
    /// </summary>
    public partial class CompactColorSelector : UserControl
    {
        #region SelectedColor Dependency Property

        public int SelectedColor
        {
            get => (int)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                nameof(SelectedColor),
                typeof(int),
                typeof(CompactColorSelector),
                new FrameworkPropertyMetadata(
                    defaultValue: Showlight.FogMin,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    SelectedColorPropertyChanged)
                );

        private static void SelectedColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            int newValue = (int)e.NewValue;
            if (Showlight.GetShowlightType(newValue) == ShowlightType.Undefined)
                return;

            var colSel = d as CompactColorSelector;

            colSel.SingleColorSelectRect.Fill = colSel.SingleColorSelectEllipse.Fill = (Brush)Application.Current.Resources["SolidBrush" + newValue.ToString()];

            if (Showlight.GetShowlightType(newValue) == ShowlightType.Fog)
            {
                colSel.SingleColorSelectRect.Visibility = Visibility.Visible;
                colSel.SingleColorSelectEllipse.Visibility = Visibility.Collapsed;
            }
            else
            {
                colSel.SingleColorSelectEllipse.Visibility = Visibility.Visible;
                colSel.SingleColorSelectRect.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

        #region BeamsVisible Dependency Property

        public Visibility BeamsVisible
        {
            get => (Visibility)GetValue(BeamsVisibleProperty);
            set => SetValue(BeamsVisibleProperty, value);
        }

        public static readonly DependencyProperty BeamsVisibleProperty =
            DependencyProperty.Register(nameof(BeamsVisible), typeof(Visibility), typeof(CompactColorSelector), new PropertyMetadata(Visibility.Visible));

        #endregion

        public event EventHandler SelectedColorChanged;

        public CompactColorSelector()
        {
            InitializeComponent();
        }

        private void ColorSelectButton_Click(object sender, MouseButtonEventArgs e)
        {
            ColorSelectPopUp.IsOpen = true;
            e.Handled = true;
        }

        private void ColorSelectPopUp_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Shape)
            {
                ColorSelectPopUp.IsOpen = false;

                int oldColor = SelectedColor;
                SelectedColor = PART_ColorSelector.SelectedColor;

                if (SelectedColor != oldColor)
                    SelectedColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SingleColorSelectButton_Click(object sender, RoutedEventArgs e)
        {
            ColorSelectPopUp.IsOpen = true;
        }
    }
}
