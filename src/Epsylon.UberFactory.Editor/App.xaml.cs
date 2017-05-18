using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Epsylon.UberFactory
{    
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DialogHooks.SetFileDialogHook(_Dialogs.ShowOpenFileDialog);
            DialogHooks.SetDirectoryDialogHook(_Dialogs.ShowBrowseDirectoryDialog);
            DialogHooks.SetColorPickerDialogHook(_Dialogs.ShowColorPickerDialog);

            RecentFilesManager.UseXmlPersister();

            base.OnStartup(e);            
        }
    }
}
