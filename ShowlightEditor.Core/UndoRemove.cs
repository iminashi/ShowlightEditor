using DynamicData;
using ShowlightEditor.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ShowlightEditor.Core
{
    public sealed class UndoRemove : IUndoable<Showlight>
    {
        private readonly IEnumerable<Showlight> removedShowlights;
        private readonly ISourceCache<Showlight, int> data;

        public string Description { get; }

        public UndoRemove(string description, ISourceCache<Showlight, int> data, IEnumerable<Showlight> removedShowlights)
        {
            Description = description;

            this.removedShowlights = removedShowlights;
            this.data = data;
        }

        public Showlight Redo()
        {
            data.Remove(removedShowlights);

            return null;
        }

        public Showlight Undo()
        {
            data.AddOrUpdate(removedShowlights);

            return removedShowlights.FirstOrDefault();
        }
    }
}
