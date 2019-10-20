using Microsoft.Win32;
using ShowlightEditor.Core.Models;
using ShowlightEditor.Core.Services;
using System.Collections.Generic;
using System.Windows;

namespace ShowlightEditor.WPF.Services
{
    public class WPFServices : IPlatformSpecificServices
    {
        public const string ClipboardDataFormat = "ShowlightList";

        public void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool? QueryUser(string message, string title)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                return true;
            }
            else if (result == MessageBoxResult.No)
            {
                return false;
            }
            else
            {
                return null;
            }
        }

        public string OpenFileDialog(string title, string filter)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                Multiselect = false
            };

            return openDialog.ShowDialog() == true ? openDialog.FileName : null;
        }

        public bool? SaveFileDialog(ref string filename, string filter)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = filename
            };

            bool? result = saveDialog.ShowDialog();
            filename = saveDialog.FileName;
            return result;
        }

        public void SetClipBoardData(List<Showlight> data)
        {
            Clipboard.SetData(ClipboardDataFormat, data);
        }

        public List<Showlight> GetClipBoardData()
        {
            return Clipboard.GetData(ClipboardDataFormat) as List<Showlight>;
        }
    }
}
