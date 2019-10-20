using ShowlightEditor.Core.Models;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;

namespace ShowlightEditor.Core
{
    public static class UndoManager
    {
        public static IObservable<Showlight> AffectedShowlight => affectedShowlight;
        public static IObservable<bool> UndoAvailable => undoAvailable;
        public static IObservable<bool> RedoAvailable => redoAvailable;
        public static IObservable<string> UndoDescription => undoDescription;
        public static IObservable<string> RedoDescription => redoDescription;
        public static IObservable<Unit> FileIsClean => fileIsClean;

        private static readonly Stack<IUndoable> undoStack = new Stack<IUndoable>();
        private static readonly Stack<IUndoable> redoStack = new Stack<IUndoable>();

        private static readonly Subject<Showlight> affectedShowlight = new Subject<Showlight>();
        private static readonly Subject<bool> undoAvailable = new Subject<bool>();
        private static readonly Subject<bool> redoAvailable = new Subject<bool>();
        private static readonly Subject<string> undoDescription = new Subject<string>();
        private static readonly Subject<string> redoDescription = new Subject<string>();
        private static readonly Subject<Unit> fileIsClean = new Subject<Unit>();

        private static IUndoable undoCleanAction;
        private static IUndoable redoCleanAction;

        public static void AddUndo(IUndoable action, bool fileDirty)
        {
            if (!fileDirty)
                undoCleanAction = action;

            AddUndoInternal(action);
            ClearRedo();
        }

        public static void AddDelegateUndo(string description, Func<Showlight> undoAction, Func<Showlight> redoAction, bool fileDirty)
        {
            AddUndo(new DelegateUndo(description, undoAction, redoAction), fileDirty);
        }

        private static void AddUndoInternal(IUndoable action)
        {
            undoStack.Push(action);

            if (undoStack.Count == 1)
                undoAvailable.OnNext(true);

            undoDescription.OnNext(action.Description);
        }

        private static void AddRedo(IUndoable action)
        {
            redoStack.Push(action);

            if (redoStack.Count == 1)
                redoAvailable.OnNext(true);

            redoDescription.OnNext(action.Description);
        }

        public static void FileWasSaved()
        {
            // Undoing the last redo action will clean the file
            if (redoStack.Count > 0)
                undoCleanAction = redoStack.Peek();

            // Redoing the last undo action will clean the file
            if (undoStack.Count > 0)
                redoCleanAction = undoStack.Peek();
        }

        public static void Clear()
        {
            undoCleanAction = null;
            redoCleanAction = null;
            ClearRedo();
            ClearUndo();
        }

        private static void ClearUndo()
        {
            if (undoStack.Count > 0)
            {
                undoStack.Clear();
                NotifyUndoUnavailable();
            }
        }

        private static void ClearRedo()
        {
            if (redoStack.Count > 0)
            {
                redoStack.Clear();
                NotifyRedoUnavailable();
            }
        }

        private static void NotifyUndoUnavailable()
        {
            undoAvailable.OnNext(false);
            undoDescription.OnNext(string.Empty);
        }

        private static void NotifyRedoUnavailable()
        {
            redoAvailable.OnNext(false);
            redoDescription.OnNext(string.Empty);
        }

        public static void Undo()
        {
            if (undoStack.Count == 0)
                return;

            IUndoable action = undoStack.Pop();
            Showlight sl = action.Undo();

            if (ReferenceEquals(action, undoCleanAction))
                fileIsClean.OnNext(Unit.Default);

            affectedShowlight.OnNext(sl);
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

        public static void Redo()
        {
            if (redoStack.Count == 0)
                return;

            IUndoable action = redoStack.Pop();
            Showlight sl = action.Redo();

            if (ReferenceEquals(action, redoCleanAction))
                fileIsClean.OnNext(Unit.Default);

            affectedShowlight.OnNext(sl);
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
