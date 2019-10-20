using DynamicData;
using ShowlightEditor.Core.Models;
using System.Collections.Generic;

namespace ShowlightEditor.Core
{
    public class UndoEdit : IUndoable<Showlight>
    {
        private readonly List<(Showlight editedShowlight, int oldNote)> oldShowlights = new List<(Showlight, int)>();
        private readonly int newNote;

        public string Description => "Edit";

        public UndoEdit(List<(Showlight, int)> oldShowlights, int newNote)
        {
            this.oldShowlights = oldShowlights;
            this.newNote = newNote;
        }

        public Showlight Redo()
        {
            foreach (var (editedShowlight, _) in oldShowlights)
            {
                editedShowlight.Note = newNote;
            }

            return oldShowlights[0].editedShowlight;
        }

        public Showlight Undo()
        {
            foreach (var (editedShowlight, oldNote) in oldShowlights)
            {
                editedShowlight.Note = oldNote;
            }

            return oldShowlights[0].editedShowlight;
        }
    }
}
