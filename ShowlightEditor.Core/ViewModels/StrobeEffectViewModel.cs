using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ShowlightEditor.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;

namespace ShowlightEditor.Core.ViewModels
{
    public sealed class StrobeEffectViewModel : DialogViewModelBase, INotifyDataErrorInfo
    {
        [Reactive]
        public int Color1 { get; set; }

        [Reactive]
        public int Color2 { get; set; }

        [Reactive]
        public float StartTime { get; set; }

        [Reactive]
        public float EndTime { get; set; }

        [Reactive]
        public int FrequencyInMs { get; set; } = 33;

        public List<Showlight> GeneratedShowlights { get; set; }

        public ReactiveCommand<Unit, List<Showlight>> Generate { get; set; }

        public StrobeEffectViewModel()
        {
            var canGenerate = this.WhenAnyValue(
                x => x.Color1,
                x => x.Color2,
                x => x.HasErrors,
                (c1, c2, err) => !err && c1 != c2 && Showlight.GetShowlightType(c1) == Showlight.GetShowlightType(c2));

            this.WhenAnyValue(x => x.StartTime, x => x.EndTime)
                .Subscribe(tuple =>
                {
                    var (startTime, endTime) = tuple;
                    bool errorsChanged = false;

                    if (_errors.ContainsKey(nameof(EndTime)))
                    {
                        _errors.Remove(nameof(EndTime));
                        errorsChanged = true;
                    }

                    if (endTime <= startTime)
                    {
                        _errors.Add(nameof(EndTime), "End time cannot be less than start time.");
                        errorsChanged = true;
                    }

                    if (errorsChanged)
                    {
                        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(EndTime)));
                        this.RaisePropertyChanged(nameof(HasErrors));
                    }
                });

            Generate = ReactiveCommand.Create(GenerateStrobeEffect, canGenerate);
            Generate.BindTo(this, x => x.GeneratedShowlights);
        }

        private List<Showlight> GenerateStrobeEffect()
        {
            var generated = new List<Showlight>();

            ShowlightType type = Showlight.GetShowlightType(Color1);
            float frequency = FrequencyInMs / 1000.0f;
            bool switcher = true;

            for (float time = StartTime; time < EndTime; time += frequency)
            {
                generated.Add(new Showlight(switcher ? Color1 : Color2, (float)Math.Round(time, 3, MidpointRounding.AwayFromZero)));

                switcher = !switcher;
            }

            Hide(result: true);

            return generated;
        }

        private readonly Dictionary<string, string> _errors = new Dictionary<string, string>();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public bool HasErrors => _errors.Count > 0;

        public IEnumerable GetErrors(string propertyName)
        {
            if (_errors.TryGetValue(propertyName, out string error))
                return Enumerable.Repeat(error, 1);
            else
                return null;
        }
    }
}
