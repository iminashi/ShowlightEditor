using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;

namespace ShowlightEditor.Core.ViewModels
{
    public sealed class LaserLightsViewModel : DialogViewModelBase
    {
        [Reactive]
        public int OnTime { get; set; }

        [Reactive]
        public int OffTime { get; set; }

        public ReactiveCommand<Unit, Unit> Set { get; set; }

        public LaserLightsViewModel()
        {
            var canSet = this.WhenAnyValue(
                x => x.OnTime,
                x => x.OffTime,
                (on, off) => on >= 0f && off > on);

            Set = ReactiveCommand.Create(() => Hide(result: true), canSet);
        }
    }
}
