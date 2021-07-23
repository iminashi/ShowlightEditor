using ShowlightEditor.Core.ViewModels;

namespace ShowlightEditor.Core
{
    public sealed class UndoMove : IUndoable<ShowLightViewModel>
    {
        private readonly ShowLightViewModel showlight;
        private readonly int newTime;
        private readonly int oldTime;

        public string Description => "Move";

        public UndoMove(ShowLightViewModel showlight, int oldTime, int newTime)
        {
            this.showlight = showlight;
            this.oldTime = oldTime;
            this.newTime = newTime;
        }

        public ShowLightViewModel Redo()
        {
            showlight.Time = newTime;

            return showlight;
        }

        public ShowLightViewModel Undo()
        {
            showlight.Time = oldTime;

            return showlight;
        }
    }
}
