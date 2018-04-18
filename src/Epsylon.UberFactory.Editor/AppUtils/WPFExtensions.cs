using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    static class WPFExtensions
    {
        public static void RestartApplication(this System.Reflection.Assembly assembly, System.Windows.Window wnd, params string[] args)
        {
            var wbounds = wnd.GetWindowStatusForCLI();

            args = new string[] { wbounds }.Concat(args).ToArray();

            assembly.RestartApplication(args);
        }

        // Runs a new instance of the program by command line after 1 second delay. During the delay current instance shutdown.
        public static void RestartApplication(this System.Reflection.Assembly assembly, params string[] args)
        {
            // https://stackoverflow.com/a/44477612

            // TODO: Pass current window bounds to force display in the same location.            

            var appPath = assembly.Location;

            var exeArgs = string.Join(" ", args);

            exeArgs = $"/C choice /C Y /N /D Y /T 1 & START \"\" \"{appPath}\" {exeArgs}";

            var Info = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = exeArgs,

                // these properties apply to CMD.EXE, NOT to our application.
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            System.Diagnostics.Process.Start(Info);

            System.Windows.Application.Current.Shutdown();
        }
    }
}
