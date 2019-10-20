using ReactiveUI;
using ShowlightEditor.Core;
using ShowlightEditor.Core.Models;
using System;
using System.Windows;

namespace ShowlightEditor.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //private readonly AutoSuspendHelper autoSuspendHelper;

        public App()
        {
            //autoSuspendHelper = new AutoSuspendHelper(this);
            //RxApp.SuspensionHost.CreateNewAppState = () => new GenerationPreferences();
            //RxApp.SuspensionHost.SetupDefaultSuspendResume(new SuspensionDriver());
        }
    }
}
