using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    static class _AppConstants
    {
        static _AppConstants()
        {
            _CLIArgs = System.Environment.GetCommandLineArgs();
        }

        private static readonly string[] _CLIArgs;

        public static PathString? StartupOpenDocumentPath
        {
            get
            {
                if (_CLIArgs.Length == 0) return null;

                var last = _CLIArgs.Last();

                var docPath = new PathString(last);
                if (!docPath.FileExists) return null;
                if (!docPath.HasExtension("uberfactory")) return null;

                return docPath;
            }
        }
    }
}
