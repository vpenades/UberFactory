using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    // AppDomain.Load > RuntimeAssembly.InternalLoadAssemblyName
    // Assembly.LoadFrom > RuntimeAssembly.InternalLoadFrom > RuntimeAssembly.InternalLoadAssemblyName > nLoad


    using ASSEMBLYRESOLVEFUNC = Func<System.Reflection.AssemblyName, System.Reflection.Assembly>;

    public static class AssemblyContext
    {
        // http://www.michael-whelan.net/replacing-appdomain-in-dotnet-core/

        // Project frameworks require multitarget: <TargetFrameworks>netcoreapp1.1;net462</TargetFrameworks>

        // Assembly Loading can only be done with two APIs:
        // Net.Framework uses System.AppDomain
        // Net.Core uses System.Runtime.Loader.AssemblyLoadContext which requires System.Runtime.Loader NuGet Package

        // NOTE: although System.Runtime.Loader is marked as compatible with Net.Standard 1.5, it is only implemented for Net.Core        



        

        #if NET462 || NETCOREAPP1_1
        private static readonly Object _LockObject = new object();
        private static ASSEMBLYRESOLVEFUNC _AssemblySolver;
        #else
        private const string _NotSupportedExceptionMsg = "Assembly loading only available for Net.Framework and Net.Core";
        #endif


        public static System.Reflection.Assembly GetEntryAssembly()
        {
            #if NET462 || NETCOREAPP1_1

            return System.Reflection.Assembly.GetEntryAssembly();

            #else

            throw new NotSupportedException(_NotSupportedExceptionMsg);

            #endif
        }

        public static string GetDirectory(this System.Reflection.Assembly assembly)
        {
            #if NET462 || NETCOREAPP1_1

            return System.IO.Path.GetDirectoryName(assembly.Location);            
        
            #else

            throw new NotSupportedException(_NotSupportedExceptionMsg);

            #endif
        }

        public static System.Reflection.Assembly[] GetLoadedAssemblies()
        {
            #if NET462

            return System.AppDomain.CurrentDomain.GetAssemblies();

            #elif NETCOREAPP1_1
            // https://github.com/dotnet/coreclr/blob/master/src/mscorlib/src/System/Runtime/Loader/AssemblyLoadContext.cs#L74            
            // return System.Runtime.Loader.AssemblyLoadContext.GetLoadedAssemblies();
            throw new NotImplementedException();
            
            #else

            throw new NotSupportedException(_NotSupportedExceptionMsg);

            #endif
        }


        public static System.Reflection.AssemblyName GetAssemblyName(string absoluteFilePath)
        {
            #if NET462

            return System.Reflection.AssemblyName.GetAssemblyName(absoluteFilePath);

            #elif NETCOREAPP1_1

            return System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName(absoluteFilePath);   
        
            #else

            throw new NotSupportedException(_NotSupportedExceptionMsg);

            #endif
        }        

        public static System.Reflection.Assembly LoadAssemblyFromFilePath(string absoluteFilePath)
        {
            // Note that MEF uses Assembly.Load(AssemblyName.GetAssemblyName(absPath));
            // http://stackoverflow.com/questions/1477843/difference-between-loadfile-and-loadfrom-with-net-assemblies/1477900#1477900
            // Conclusion: better uses LoadFrom always

            #if NET462

            if (!System.IO.File.Exists(absoluteFilePath)) return null;

            return System.Reflection.Assembly.LoadFrom(absoluteFilePath);            

            #elif NETCOREAPP1_1

            return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(absoluteFilePath);

            #else

            throw new NotSupportedException(_NotSupportedExceptionMsg);

            #endif
        }        


        public static void SetAssemblyResolver(ASSEMBLYRESOLVEFUNC func)
        {
            // https://msdn.microsoft.com/en-us/library/ff527268.aspx

            func = func == null ? (ASSEMBLYRESOLVEFUNC)null : n => _AssemblyResolve(n, func);

            #if NET462

            lock (_LockObject)
            {
                if (func != null && _AssemblySolver == null) System.AppDomain.CurrentDomain.AssemblyResolve += _AssemblyResolve;
                if (func == null && _AssemblySolver != null) System.AppDomain.CurrentDomain.AssemblyResolve -= _AssemblyResolve;

                _AssemblySolver = func;

                return;
            }

            #elif NETCOREAPP1_1

            lock(_LockObject)
            {

                if (func != null && _AssemblySolver == null) System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += _AssemblyResolve;
                if (func == null && _AssemblySolver != null) System.Runtime.Loader.AssemblyLoadContext.Default.Resolving -= _AssemblyResolve;                                

                _AssemblySolver = func;

                return;
            }
        
            #else

            throw new NotSupportedException(_NotSupportedExceptionMsg);            

            #endif
        }


        private static System.Reflection.Assembly _AssemblyResolve(System.Reflection.AssemblyName aname, ASSEMBLYRESOLVEFUNC fallbackFunc)
        {
            // note: after Net 4.0 onwards, satellite assembly resources are also resolved here!
                       
            try
            {
                var dllPath = System.IO.Path.Combine(GetEntryAssembly().GetDirectory(), aname.Name + ".dll");
                return LoadAssemblyFromFilePath(dllPath);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Failed to find " + aname.Name + " in Application directory");
            }

            try
            {
                return fallbackFunc(aname);
            }
            catch            
            {
                System.Diagnostics.Debug.WriteLine("Failed to find " + aname.Name + " in fallback directories");
            }            

            return null; 
        }

        #if NET462

        private static System.Reflection.Assembly _AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var aname = new System.Reflection.AssemblyName(args.Name);

            lock (_LockObject) { return _AssemblySolver(aname); }            
        }

        #elif NETCOREAPP1_1

        private static System.Reflection.Assembly _AssemblyResolve(System.Runtime.Loader.AssemblyLoadContext ctx, System.Reflection.AssemblyName aname)
        {
            lock (_LockObject) { return _AssemblySolver(aname); }            
        }
        
        #endif
    }
}
