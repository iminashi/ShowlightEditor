using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Rocksmith2014.XML;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace ShowlightEditor.Core.ViewModels
{
    public sealed class ReplaceViewModel : DialogViewModelBase
    {
        [Reactive]
        public IEnumerable<ShowLightViewModel> OriginalShowlights { get; set; }

        public IEnumerable<ShowLightViewModel> SelectedShowlights { get; set; }

        [Reactive]
        public int OriginalColor { get; set; } = ShowLight.FogMin;

        [Reactive]
        public int ReplaceWithColor { get; set; } = ShowLight.FogMin;

        [Reactive]
        public bool SelectionOnly { get; set; }

        [Reactive]
        public bool SelectionOnlyEnabled { get; set; }

        [Reactive]
        public string ChangesText { get; set; }

        public ReactiveCommand<Unit, Unit> Replace { get; set; }

        public ReplaceViewModel()
        {
            this.WhenAnyValue(x => x.SelectionOnlyEnabled)
                .Where(se => !se)
                .Subscribe(_ => SelectionOnly = false);

            var canReplace = this.WhenAnyValue(
                x => x.OriginalColor,
                x => x.ReplaceWithColor,
                (orig, rep) => orig != rep);

            Replace = ReactiveCommand.Create(() => Hide(result: true), canReplace);

            this.WhenAnyValue(
                x => x.OriginalColor,
                x => x.SelectionOnly,
                x => x.OriginalShowlights)
                .Where(t => t.Item3 is not null)
                .Subscribe(tuple =>
                {
                    (int originalColor, bool selOnly, IEnumerable<ShowLightViewModel> original) = tuple;

                    var showlights = selOnly ? SelectedShowlights : original;
                    int count = showlights.Count(x => x.Note == originalColor);

                    ChangesText = $"Will change {count} Showlight{(count == 1 ? "" : "s")}";
                });
        }
    }
}
