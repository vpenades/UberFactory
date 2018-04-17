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
            RecentFilesManager.UseXmlPersister();

            base.OnStartup(e);            
        }

        public void Restart()
        {
            var wbounds = _GetWindowBounds(this.MainWindow);

            _RunApplication(System.Reflection.Assembly.GetEntryAssembly().Location);

            this.Shutdown();
        }

        public void RestartAndLoad(PathString docPath)
        {
            _Dialogs.ShowRestartDialog(docPath);

            var fpath = $"\"{docPath.AsAbsolute()}\"";

            var wbounds = _GetWindowBounds(this.MainWindow);

            _RunApplication(System.Reflection.Assembly.GetEntryAssembly().Location, fpath, wbounds);

            this.Shutdown();
        }

        private static string _GetWindowBounds(Window wnd)
        {
            var b = wnd.RestoreBounds;

            return $"-WBOUNDS:{b.X}:{b.Y}:{b.Width}:{b.Height}";
        }

        // Runs a new instance of the program by command line after 1 second delay. During the delay current instance shutdown.
        private static void _RunApplication(string appPath, params string[] args)
        {
            // https://stackoverflow.com/a/44477612

            // TODO: Pass current window bounds to force display in the same location.            

            var exeArgs = string.Join(" ", args);

            exeArgs = $"/C choice /C Y /N /D Y /T 1 & START \"\" \"{appPath}\" {exeArgs}";

            var Info = new System.Diagnostics.ProcessStartInfo
            {
                Arguments = exeArgs,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };

            System.Diagnostics.Process.Start(Info);
        }
    }
}
