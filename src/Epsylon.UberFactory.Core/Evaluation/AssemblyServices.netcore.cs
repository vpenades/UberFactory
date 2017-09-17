using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
{
    using ASSEMBLYRESOLVEFUNC = Func<AssemblyName, Assembly>;

    public static partial class AssemblyServices
    {
        // http://www.michael-whelan.net/replacing-appdomain-in-dotnet-core/

        // NOTE: although System.Runtime.Loader is declared as compatible with Net.Standard 1.5, it is only implemented for Net.Core

        public static Assembly GetEntryAssembly() { return Assembly.GetEntryAssembly(); }

        public static string GetDirectory(this Assembly assembly) { return System.IO.Path.GetDirectoryName(assembly.Location); }

        public static Assembly[] GetLoadedAssemblies()
        {
            // https://github.com/dotnet/coreclr/blob/master/src/mscorlib/src/System/Runtime/Loader/AssemblyLoadContext.cs#L74            
            // return AssemblyLoadContext.GetLoadedAssemblies();

            throw new NotSupportedException("DotNet API says it should be supported by now, but it is not yet.");
        }

        public static AssemblyName GetAssemblyName(string absoluteFilePath)
        {
            if (!System.IO.File.Exists(absoluteFilePath)) return null;

            return AssemblyLoadContext.GetAssemblyName(absoluteFilePath);
        }

        public static Assembly LoadAssemblyFromFilePath(string absoluteFilePath)
        {
            if (!System.IO.File.Exists(absoluteFilePath)) return null;

            return AssemblyLoadContext.Default.LoadFromAssemblyPath(absoluteFilePath);
        }

        public static void SetDefaultAssemblyResolver(ASSEMBLYRESOLVEFUNC func)
        {
            // https://msdn.microsoft.com/en-us/library/ff527268.aspx            
            
            if (func != null && _AssemblySolver == null) AssemblyLoadContext.Default.Resolving += _AssemblyResolveAdapter;
            if (func == null && _AssemblySolver != null) AssemblyLoadContext.Default.Resolving -= _AssemblyResolveAdapter;            

            _AssemblySolver = func;

            return;            
        }

        private static Assembly _AssemblyResolveAdapter(AssemblyLoadContext ctx, AssemblyName aname)
        {
            if (ctx != AssemblyLoadContext.Default) throw new ArgumentException("AssemblyLoadContexts other than default not supported", nameof(ctx));

            return _AssemblySolver?.Invoke(aname);
        }
    }    
}
