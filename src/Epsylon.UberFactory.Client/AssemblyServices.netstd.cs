using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
{
    using ASSEMBLYRESOLVEFUNC = Func<AssemblyName, Assembly>;

    public static partial class AssemblyServices
    {
        public static Assembly GetEntryAssembly() { return null; }

        public static string GetDirectory(this Assembly assembly) { throw new PlatformNotSupportedException(); }

        public static Assembly[] GetLoadedAssemblies() { throw new PlatformNotSupportedException(); }

        public static AssemblyName GetAssemblyName(string absoluteFilePath) { throw new PlatformNotSupportedException(); }

        public static Assembly LoadAssemblyFromFilePath(string absoluteFilePath) { throw new PlatformNotSupportedException(); }

        public static void SetDefaultAssemblyResolver(ASSEMBLYRESOLVEFUNC func) { throw new PlatformNotSupportedException(); }
    }
}
