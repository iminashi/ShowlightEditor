using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ShowlightEditor.Core.Models;
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
        public IEnumerable<Showlight> OriginalShowlights { get; set; }

        public IEnumerable<Showlight> SelectedShowlights { get; set; }

        [Reactive]
        public int OriginalColor { get; set; } = Showlight.FogMin;

        [Reactive]
        public int ReplaceWithColor { get; set; } = Showlight.FogMin;

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
                .Where(t => t.Item3 != null)
                .Subscribe(tuple =>
                {
                    (int originalColor, bool selOnly, IEnumerable<Showlight> original) = tuple;

                    IEnumerable<Showlight> showlights = selOnly ? SelectedShowlights : original;
                    int count = showlights.Count(x => x.Note == originalColor);

                    ChangesText = $"Will change {count} Showlight{(count == 1 ? "" : "s")}";
                });
        }
    }
}
