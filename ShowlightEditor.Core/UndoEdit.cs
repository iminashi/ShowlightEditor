using DynamicData;
using ShowlightEditor.Core.Models;
using System.Collections.Generic;

namespace ShowlightEditor.Core
{
    public class UndoEdit : IUndoable
    {
        public readonly List<(Showlight editedShowlight, int oldNote)> OldShowLights = new List<(Showlight, int)>();

        private readonly ISourceCache<Showlight, int> data;
        private readonly int newNote;

        public string Description => "Edit";

        public UndoEdit(ISourceCache<Showlight, int> data, int newNote)
        {
            this.data = data;
            this.newNote = newNote;
        }

        public Showlight Redo()
        {
            foreach (var (editedShowlight, _) in OldShowLights)
            {
                editedShowlight.Note = newNote;
            }

            return OldShowLights[0].editedShowlight;
        }

        public Showlight Undo()
        {
            foreach (var (editedShowlight, oldNote) in OldShowLights)
            {
                editedShowlight.Note = oldNote;
            }

            return OldShowLights[0].editedShowlight;
        }
    }
}
