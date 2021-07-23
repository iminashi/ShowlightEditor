using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive;

namespace ShowlightEditor.Core.ViewModels
{
    public sealed class TimeShifterViewModel : DialogViewModelBase
    {
        [Reactive]
        public float ShiftAmount { get; set; }

        public ReactiveCommand<Unit, Unit> Shift { get; set; }

        public TimeShifterViewModel()
        {
            Shift = ReactiveCommand.Create(() => Hide(result: true));
        }
    }
}
