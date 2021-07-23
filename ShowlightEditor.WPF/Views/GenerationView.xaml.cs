using ShowlightEditor.Core.ViewModels;

using ShowLightGenerator;

using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShowlightEditor.WPF.Views
{
    /// <summary>
    /// Interaction logic for GenerationView.xaml
    /// </summary>
    public partial class GenerationView : UserControl
    {
        private GenerationViewModel ViewModel { get; set; }

        public GenerationView()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ViewModel = e.NewValue as GenerationViewModel;

            if (ViewModel is null)
                return;

            ViewModel.Generate
                .IsExecuting
                .ObserveOnDispatcher()
                .Subscribe(executing => Cursor = executing ? Cursors.Wait : Cursors.Arrow);

            switch (ViewModel.Preferences.FogMethod)
            {
                case FogGenerationMethod.ChangeEveryNthBar:
                    fogNthBarRB.IsChecked = true;
                    break;
                case FogGenerationMethod.MinTimeBetweenChanges:
                    fogMinTimeRB.IsChecked = true;
                    break;
                case FogGenerationMethod.FromSectionNames:
                    fogPerSectionRB.IsChecked = true;
                    break;
                case FogGenerationMethod.FromChords:
                    fogChordsRB.IsChecked = true;
                    break;
                case FogGenerationMethod.FromLowestOctaveNotes:
                    fogLowOctaveRB.IsChecked = true;
                    break;
            }

            switch (ViewModel.Preferences.BeamMethod)
            {
                case BeamGenerationMethod.FollowFogNotes:
                    beamFollowFogRB.IsChecked = true;
                    break;
                case BeamGenerationMethod.MinTimeBetweenChanges:
                    beamMinTimeRB.IsChecked = true;
                    break;
            }
        }
    }
}
