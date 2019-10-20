using ShowlightEditor.Core.Models;
using System;

namespace ShowlightEditor.Core
{
    public class DelegateUndo : IUndoable
    {
        private readonly Func<Showlight> undoAction;
        private readonly Func<Showlight> redoAction;

        public string Description { get; }

        public DelegateUndo(string description, Func<Showlight> undoAction, Func<Showlight> redoAction)
        {
            Description = description;
            this.undoAction = undoAction;
            this.redoAction = redoAction;
        }

        public Showlight Redo() => redoAction();

        public Showlight Undo() => undoAction();
    }
}
