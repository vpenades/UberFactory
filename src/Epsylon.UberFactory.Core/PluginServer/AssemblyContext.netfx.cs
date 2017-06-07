using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    using ASSEMBLYRESOLVEFUNC = Func<AssemblyName, Assembly>;

    public static partial class AssemblyContext
    {
        public static Assembly GetEntryAssembly() { return Assembly.GetEntryAssembly(); }

        public static string GetDirectory(this Assembly assembly) { return System.IO.Path.GetDirectoryName(assembly.Location); }

        public static Assembly[] GetLoadedAssemblies() { return AppDomain.CurrentDomain.GetAssemblies(); }


        public static AssemblyName GetAssemblyName(string absoluteFilePath) { return AssemblyName.GetAssemblyName(absoluteFilePath); }

        public static Assembly LoadAssemblyFromFilePath(string absoluteFilePath)
        {
            // Note that MEF uses Assembly.Load(AssemblyName.GetAssemblyName(absPath));
            // http://stackoverflow.com/questions/1477843/difference-between-loadfile-and-loadfrom-with-net-assemblies/1477900#1477900
            // Conclusion: better uses LoadFrom always

            if (!System.IO.File.Exists(absoluteFilePath)) return null;

            return Assembly.LoadFrom(absoluteFilePath);
        }

        public static void SetAssemblyResolver(ASSEMBLYRESOLVEFUNC func)
        {
            // https://msdn.microsoft.com/en-us/library/ff527268.aspx

            func = func == null ? (ASSEMBLYRESOLVEFUNC)null : n => _AssemblyResolve(n, func);

            lock (_LockObject)
            {
                if (func != null && _AssemblySolver == null) AppDomain.CurrentDomain.AssemblyResolve += _AssemblyResolve;
                if (func == null && _AssemblySolver != null) AppDomain.CurrentDomain.AssemblyResolve -= _AssemblyResolve;

                _AssemblySolver = func;

                return;
            }
        }

        private static Assembly _AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var aname = new AssemblyName(args.Name);
            return _AssemblyResolve(aname);
        }
    }
}
