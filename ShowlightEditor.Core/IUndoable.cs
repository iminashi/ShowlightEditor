namespace ShowlightEditor.Core
{
    public interface IUndoable<T>
    {
        string Description { get; }

        T Undo();
        T Redo();
    }
}
