using ShowlightEditor.Core.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace ShowlightEditor.WPF.Controls
{
    /// <summary>
    /// Interaction logic for ColorSelector.xaml
    /// </summary>
    public partial class ColorSelector : UserControl
    {
        #region SelectedColor Dependency Property

        public int SelectedColor
        {
            get => (int)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(int), typeof(ColorSelector), new PropertyMetadata(Showlight.FogMin));

        #endregion

        #region BeamsVisible Dependency Property

        public Visibility BeamsVisible
        {
            get => (Visibility)GetValue(BeamsVisibleProperty);
            set => SetValue(BeamsVisibleProperty, value);
        }

        public static readonly DependencyProperty BeamsVisibleProperty =
            DependencyProperty.Register(nameof(BeamsVisible), typeof(Visibility), typeof(ColorSelector), new PropertyMetadata(Visibility.Visible));

        #endregion

        #region Command Dependency Property

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ColorSelector), new PropertyMetadata());

        #endregion

        #region CommandParameter Dependency Property

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(ColorSelector), new PropertyMetadata());

        #endregion

        public ColorSelector()
        {
            InitializeComponent();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Shape shape)
            {
                SelectedColor = Convert.ToInt32(shape.Tag); //SelectedColor = int.Parse(shape.Tag.ToString());
                if(Command?.CanExecute(CommandParameter) == true)
                    Command.Execute(CommandParameter);
            }
        }
    }
}
