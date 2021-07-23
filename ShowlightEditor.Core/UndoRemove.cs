using DynamicData;

using ShowlightEditor.Core.ViewModels;

using System.Collections.Generic;
using System.Linq;

namespace ShowlightEditor.Core
{
    public sealed class UndoRemove : IUndoable<ShowLightViewModel>
    {
        private readonly IEnumerable<ShowLightViewModel> removedShowlights;
        private readonly ISourceCache<ShowLightViewModel, int> data;

        public string Description { get; }

        public UndoRemove(string description, ISourceCache<ShowLightViewModel, int> data, IEnumerable<ShowLightViewModel> removedShowlights)
        {
            Description = description;

            this.removedShowlights = removedShowlights;
            this.data = data;
        }

        public ShowLightViewModel Redo()
        {
            data.Remove(removedShowlights);

            return null;
        }

        public ShowLightViewModel Undo()
        {
            data.AddOrUpdate(removedShowlights);

            return removedShowlights.FirstOrDefault();
        }
    }
}
