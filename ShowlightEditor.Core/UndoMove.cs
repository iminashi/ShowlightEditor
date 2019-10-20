using ShowlightEditor.Core.Models;

namespace ShowlightEditor.Core
{
    public class UndoMove : IUndoable<Showlight>
    {
        private readonly Showlight showlight;
        private readonly float newTime;
        private readonly float oldTime;

        public string Description => "Move";

        public UndoMove(Showlight showlight, float oldTime, float newTime)
        {
            this.showlight = showlight;
            this.oldTime = oldTime;
            this.newTime = newTime;
        }

        public Showlight Redo()
        {
            showlight.Time = newTime;

            return showlight;
        }

        public Showlight Undo()
        {
            showlight.Time = oldTime;

            return showlight;
        }
    }
}
