using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Rocksmith2014.XML;

using ShowLightGenerator;

using ShowlightEditor.Core.Models;
using ShowlightEditor.Core.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using XmlUtils;

namespace ShowlightEditor.Core.ViewModels
{
    public sealed class GenerationViewModel : DialogViewModelBase
    {
        public const string PrefFileName = "GenerationPreferences.xml";

        private readonly IPlatformSpecificServices services;

        public GenerationPreferences Preferences { get; }

        public List<ShowLight> ShowlightsList { get; private set; }
        public IEnumerable<ShowLightViewModel> CurrentShowlights { get; set; }

        public ReactiveCommand<FogGenerationMethod, Unit> FogMethodRB { get; }
        public ReactiveCommand<BeamGenerationMethod, Unit> BeamMethodRB { get; }
        public ReactiveCommand<ShowLightType, Unit> SelectArrangement { get; }
        public ReactiveCommand<Unit, Unit> Generate { get; }

        [Reactive]
        public bool ArrangementSelected { get; private set; }

        public extern string ArrangementForBeamsText { [ObservableAsProperty]get; }
        public extern string ArrangementForFogText { [ObservableAsProperty]get; }

        [Reactive]
        public bool ShouldGenerateBeams { get; set; } = true;

        [Reactive]
        public bool ShouldGenerateFog { get; set; } = true;

        [Reactive]
        public bool ShouldGenerateLasers { get; set; } = true;

        [Reactive]
        public int SelectedSingleFogColor { get; set; } = ShowLight.FogMin;

        [Reactive]
        private string ArrangementForBeamsFilename { get; set; }

        [Reactive]
        private string ArrangementForFogFilename { get; set; }

        public GenerationViewModel(IPlatformSpecificServices services)
        {
            this.services = services;

            var preferencesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PrefFileName);

            if (File.Exists(preferencesFile))
                Preferences = GenerationPreferences.Load(preferencesFile);
            else
                Preferences = new GenerationPreferences();

            FogMethodRB = ReactiveCommand.Create<FogGenerationMethod>(method => Preferences.FogMethod = method);
            BeamMethodRB = ReactiveCommand.Create<BeamGenerationMethod>(method => Preferences.BeamMethod = method);

            SelectArrangement = ReactiveCommand.CreateFromTask<ShowLightType>(SelectArrangement_Impl);

            var canGenerate = this.WhenAnyValue(
                x => x.ShouldGenerateBeams,
                x => x.ShouldGenerateFog,
                x => x.ArrangementSelected,
                (beam, fog, selected) => selected && (beam || fog));

            Generate = ReactiveCommand.CreateFromTask(Generate_Impl, canGenerate);
            Cancel = ReactiveCommand.Create(() => Hide(result: false), Generate.IsExecuting.Select(x => !x));

            this.WhenAnyValue(x => x.ArrangementForFogFilename)
                .Where(str => str is not null)
                .Select(Path.GetFileName)
                .ToPropertyEx(this, x => x.ArrangementForFogText, "None Selected");

            this.WhenAnyValue(x => x.ArrangementForBeamsFilename)
                .Where(str => str is not null)
                .Select(Path.GetFileName)
                .ToPropertyEx(this, x => x.ArrangementForBeamsText, "None Selected");
        }

        protected override void Hide(bool result)
        {
            if (!result)
                ShowlightsList = null;

            base.Hide(result);
        }

        private async Task Generate_Impl()
        {
            var fogOptions = new FogGenerationOptions
            {
                ShouldGenerate = ShouldGenerateFog,
                ChangeFogColorEveryNthBar = Preferences.FogChangeBars,
                RandomizeColors = Preferences.FogRandomize,
                MinTimeBetweenNotes = (float)Preferences.FogMinTime,
                GenerationMethod = Preferences.FogMethod,
                SelectedSingleFogColor = (byte)SelectedSingleFogColor,
            };

            var beamOptions = new BeamGenerationOptions
            {
                ShouldGenerate = ShouldGenerateBeams,
                GenerationMethod = Preferences.BeamMethod,
                MinTimeBetweenNotes = (float)Preferences.BeamMinTime,
                RandomizeColors = Preferences.BeamRandomize,
                UseCompatibleColors = Preferences.BeamCompatibleColors
            };

            var laserOptions = new LaserGenerationOptions
            {
                ShouldGenerate = ShouldGenerateLasers,
                DisableLaserLights = Preferences.DisableLasers
            };

            var generator = new Generator(ArrangementForFogFilename, ArrangementForBeamsFilename, fogOptions, beamOptions, laserOptions);

            try
            {
                ShowlightsList = await Task.Run(() => generator.Generate(CurrentShowlights.Select(x => x.Model))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                services.ShowError(
                    "Generation failed: " + Environment.NewLine +
                    ex.Message + Environment.NewLine +
                    Environment.NewLine +
                    ex.StackTrace
                    );
            }

            Hide(result: true);
        }

        private async Task SelectArrangement_Impl(ShowLightType type)
        {
            string filename = services.OpenFileDialog($"Select Rocksmith 2014 XML File For {type} Colors Generation", "Rocksmith 2014 XML files|*.xml");

            if (filename is not null && await XmlHelper.ValidateRootElementAsync(filename, "song"))
            {
                ArrangementSelected = true;

                if (type == ShowLightType.Beam)
                {
                    ArrangementForBeamsFilename = filename;

                    if (string.IsNullOrEmpty(ArrangementForFogFilename))
                        ArrangementForFogFilename = filename;
                }
                else
                {
                    ArrangementForFogFilename = filename;

                    if (string.IsNullOrEmpty(ArrangementForBeamsFilename))
                        ArrangementForBeamsFilename = filename;
                }
            }
        }

        public void SavePreferences()
        {
            Preferences.Save(PrefFileName);
        }
    }
}
