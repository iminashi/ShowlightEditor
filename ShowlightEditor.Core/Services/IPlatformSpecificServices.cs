using ShowlightEditor.Core.ViewModels;

using System.Collections.Generic;

namespace ShowlightEditor.Core.Services
{
    public interface IPlatformSpecificServices
    {
        UserChoice QueryUser(string message, string title);

        string OpenFileDialog(string title, string filter);

        bool? SaveFileDialog(ref string filename, string filter);

        void ShowError(string message);

        void SetClipBoardData(List<ShowLightViewModel> data);

        List<ShowLightViewModel> GetClipBoardData();
    }
}
