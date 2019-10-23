using ShowlightEditor.Core.Models;
using System;

namespace ShowlightEditor.Core
{
    public sealed class DelegateUndo<T> : IUndoable<T>
    {
        private readonly Func<T> undoAction;
        private readonly Func<T> redoAction;

        public string Description { get; }

        public DelegateUndo(string description, Func<T> undoAction, Func<T> redoAction)
        {
            Description = description;
            this.undoAction = undoAction;
            this.redoAction = redoAction;
        }

        public T Redo() => redoAction();

        public T Undo() => undoAction();
    }
}
