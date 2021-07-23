using ShowlightEditor.Core.ViewModels;

using System.Collections.Generic;

namespace ShowlightEditor.Core
{
    public sealed class UndoEdit : IUndoable<ShowLightViewModel>
    {
        private readonly List<(ShowLightViewModel editedShowlight, int oldNote)> oldShowlights = new List<(ShowLightViewModel, int)>();
        private readonly int newNote;

        public string Description => "Edit";

        public UndoEdit(List<(ShowLightViewModel, int)> oldShowlights, int newNote)
        {
            this.oldShowlights = oldShowlights;
            this.newNote = newNote;
        }

        public ShowLightViewModel Redo()
        {
            foreach (var (editedShowlight, _) in oldShowlights)
            {
                editedShowlight.Note = (byte)newNote;
            }

            return oldShowlights[0].editedShowlight;
        }

        public ShowLightViewModel Undo()
        {
            foreach (var (editedShowlight, oldNote) in oldShowlights)
            {
                editedShowlight.Note = (byte)oldNote;
            }

            return oldShowlights[0].editedShowlight;
        }
    }
}
