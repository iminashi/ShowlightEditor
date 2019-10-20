using ShowlightEditor.Core.Models;
using System.Collections.Generic;

namespace ShowlightEditor.Core.Services
{
    public interface IPlatformSpecificServices
    {
        bool? QueryUser(string message, string title);

        string OpenFileDialog(string title, string filter);

        bool? SaveFileDialog(ref string filename, string filter);

        void ShowError(string message);

        void SetClipBoardData(List<Showlight> data);

        List<Showlight> GetClipBoardData();
    }
}
