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
        public static Assembly GetEntryAssembly() { return Assembly.GetEntryAssembly(); }

        public static string GetDirectory(this Assembly assembly) { return System.IO.Path.GetDirectoryName(assembly.Location); }

        public static Assembly[] GetLoadedAssemblies() { return AppDomain.CurrentDomain.GetAssemblies(); }

        public static AssemblyName GetAssemblyName(string absoluteFilePath)
        {
            if (!System.IO.File.Exists(absoluteFilePath)) return null;

            return AssemblyName.GetAssemblyName(absoluteFilePath);
        }

        public static Assembly LoadAssemblyFromFilePath(string absoluteFilePath)
        {
            // Note that MEF uses Assembly.Load(AssemblyName.GetAssemblyName(absPath));
            // http://stackoverflow.com/questions/1477843/difference-between-loadfile-and-loadfrom-with-net-assemblies/1477900#1477900
            // Conclusion: better uses LoadFrom always

            if (!System.IO.File.Exists(absoluteFilePath)) return null;

            return Assembly.LoadFrom(absoluteFilePath);
        }

        public static void SetDefaultAssemblyResolver(ASSEMBLYRESOLVEFUNC func)
        {
            // https://msdn.microsoft.com/en-us/library/ff527268.aspx            
            
            // ensure that we only receive events if we have an actual fallback function set
            if (func != null && _AssemblySolver == null) AppDomain.CurrentDomain.AssemblyResolve += _AssemblyResolveAdapter;
            if (func == null && _AssemblySolver != null) AppDomain.CurrentDomain.AssemblyResolve -= _AssemblyResolveAdapter;

            _AssemblySolver = func;                   
        }

        private static Assembly _AssemblyResolveAdapter(object sender, ResolveEventArgs args)
        {
            var aname = new AssemblyName(args.Name);
            return _AssemblySolver?.Invoke(aname);
        }
    }    
}
