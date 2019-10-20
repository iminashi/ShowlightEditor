using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;

namespace ShowlightEditor.Core
{
    public sealed class UndoManager<T>
    {
        public IObservable<T> AffectedObject => affectedObject;
        public IObservable<bool> UndoAvailable => undoAvailable;
        public IObservable<bool> RedoAvailable => redoAvailable;
        public IObservable<string> UndoDescription => undoDescription;
        public IObservable<string> RedoDescription => redoDescription;
        public IObservable<Unit> FileIsClean => fileIsClean;

        private readonly Stack<IUndoable<T>> undoStack = new Stack<IUndoable<T>>();
        private readonly Stack<IUndoable<T>> redoStack = new Stack<IUndoable<T>>();

        private readonly Subject<T> affectedObject = new Subject<T>();
        private readonly Subject<bool> undoAvailable = new Subject<bool>();
        private readonly Subject<bool> redoAvailable = new Subject<bool>();
        private readonly Subject<string> undoDescription = new Subject<string>();
        private readonly Subject<string> redoDescription = new Subject<string>();
        private readonly Subject<Unit> fileIsClean = new Subject<Unit>();

        private IUndoable<T> undoCleanAction;
        private IUndoable<T> redoCleanAction;

        public void AddUndo(IUndoable<T> action, bool fileDirty)
        {
            if (!fileDirty)
                undoCleanAction = action;

            AddUndoInternal(action);
            ClearRedo();
        }

        public void AddDelegateUndo(string description, Func<T> undoAction, Func<T> redoAction, bool fileDirty)
        {
            AddUndo(new DelegateUndo<T>(description, undoAction, redoAction), fileDirty);
        }

        private void AddUndoInternal(IUndoable<T> action)
        {
            undoStack.Push(action);

            if (undoStack.Count == 1)
                undoAvailable.OnNext(true);

            undoDescription.OnNext(action.Description);
        }

        private void AddRedo(IUndoable<T> action)
        {
            redoStack.Push(action);

            if (redoStack.Count == 1)
                redoAvailable.OnNext(true);

            redoDescription.OnNext(action.Description);
        }

        public void FileWasSaved()
        {
            // Undoing the last redo action will clean the file
            if (redoStack.Count > 0)
                undoCleanAction = redoStack.Peek();

            // Redoing the last undo action will clean the file
            if (undoStack.Count > 0)
                redoCleanAction = undoStack.Peek();
        }

        public void Clear()
        {
            undoCleanAction = null;
            redoCleanAction = null;
            ClearRedo();
            ClearUndo();
        }

        private void ClearUndo()
        {
            if (undoStack.Count > 0)
            {
                undoStack.Clear();
                NotifyUndoUnavailable();
            }
        }

        private void ClearRedo()
        {
            if (redoStack.Count > 0)
            {
                redoStack.Clear();
                NotifyRedoUnavailable();
            }
        }

        private void NotifyUndoUnavailable()
        {
            undoAvailable.OnNext(false);
            undoDescription.OnNext(string.Empty);
        }

        private void NotifyRedoUnavailable()
        {
            redoAvailable.OnNext(false);
            redoDescription.OnNext(string.Empty);
        }

        public void Undo()
        {
            if (undoStack.Count == 0)
                return;

            IUndoable<T> action = undoStack.Pop();
            T obj = action.Undo();

            if (ReferenceEquals(action, undoCleanAction))
                fileIsClean.OnNext(Unit.Default);

            affectedObject.OnNext(obj);
            AddRedo(action);

            if (undoStack.Count == 0)
            {
                NotifyUndoUnavailable();
            }
            else
            {
                undoDescription.OnNext(undoStack.Peek().Description);
            }
        }

        public void Redo()
        {
            if (redoStack.Count == 0)
                return;

            IUndoable<T> action = redoStack.Pop();
            T obj = action.Redo();

            if (ReferenceEquals(action, redoCleanAction))
                fileIsClean.OnNext(Unit.Default);

            affectedObject.OnNext(obj);
            AddUndoInternal(action);

            if (redoStack.Count == 0)
            {
                NotifyRedoUnavailable();
            }
            else
            {
                redoDescription.OnNext(redoStack.Peek().Description);
            }
        }
    }
}
