using ShowlightEditor.Core.Models;

namespace ShowlightEditor.Core
{
    public interface IUndoable
    {
        string Description { get; }

        Showlight Undo();
        Showlight Redo();
    }
}
