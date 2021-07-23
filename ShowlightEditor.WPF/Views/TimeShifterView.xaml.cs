using ShowlightEditor.WPF.Controls;

using System.Windows;
using System.Windows.Controls;

namespace ShowlightEditor.WPF.Views
{
    /// <summary>
    /// Interaction logic for TimeShifterView.xaml
    /// </summary>
    public partial class TimeShifterView : UserControl
    {
        public TimeShifterView()
        {
            InitializeComponent();
        }

#pragma warning disable IDE0051 // Remove unused private members
        private void ShiftAmount_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                _ = ((NumericUpDown)sender).Focus();
            }
        }
    }
}
