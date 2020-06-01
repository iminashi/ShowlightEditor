using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;
using System.Threading.Tasks;

namespace ShowlightEditor.Core.ViewModels
{
    public abstract class DialogViewModelBase : ReactiveObject
    {
        [Reactive]
        public bool DialogVisible { get; private set; }

        public ReactiveCommand<Unit, Unit> Cancel
        {
            get => _cancel ??= ReactiveCommand.Create(() => Hide(result: false));
            protected set => _cancel = value;
        }

        private TaskCompletionSource<bool> tcs;
        private ReactiveCommand<Unit, Unit> _cancel;

        public Task<bool> ShowDialog()
        {
            tcs = new TaskCompletionSource<bool>();

            DialogVisible = true;

            return tcs.Task;
        }

        protected virtual void Hide(bool result)
        {
            DialogVisible = false;

            tcs.SetResult(result);
        }
    }
}
