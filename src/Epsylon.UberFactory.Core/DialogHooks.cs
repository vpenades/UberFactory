using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static class DialogHooks
    {
        private static Func<String, PathString, PathString> _PickFileDialogHook;
        private static Func<PathString, PathString> _PickDirectoryDialogHook;
        private static Func<UInt32, UInt32> _PickColorDialogHook;

        public static void SetFileDialogHook(Func<String, PathString, PathString> hook) { _PickFileDialogHook = hook; }
        public static void SetDirectoryDialogHook(Func<PathString, PathString> hook) { _PickDirectoryDialogHook = hook; }
        public static void SetColorPickerDialogHook(Func<UInt32, UInt32> hook) { _PickColorDialogHook = hook; }



        public static PathString ShowFilePickerDialog(string fileFilter, PathString startDir) { return _PickFileDialogHook(fileFilter, startDir); }

        public static PathString ShowDirectoryPickerDialog(PathString startDir) { return _PickDirectoryDialogHook(startDir); }

        public static UInt32 ShowColorPickerDialog(UInt32 color) { return _PickColorDialogHook(color); }
    }
}
