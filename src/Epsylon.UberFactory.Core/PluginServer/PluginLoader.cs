using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;



namespace Epsylon.UberFactory
{
    // // https://mef.codeplex.com/
    // MEF is a part of the Microsoft.NET Framework, with types primarily under the
    // System..Composition.namespaces.There are two versions of MEF:

    // System.ComponentModel.Composition.* which has shipped with.NET 4.0 and higher and
    // Silverlight 4. This provides the standard extension model that has been used in
    // Visual Studio.The documentation for this version of MEF can be found here:

    // System.Compostion.* _ is a lightweight version of MEF, which has been optimized for
    // static composition scenarios and provides faster compositions.It is also the only
    // version of MEF that is as a portable class library and can be used on phone, store,
    // desktop and web applications.This version of MEF is available via NuGet and is documentation
    // is available here: https://msdn.microsoft.com/en-us/library/jj635137%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
    // https://weblogs.asp.net/ricardoperes/using-mef-in-net-core

    

    // https://social.msdn.microsoft.com/Forums/en-US/eaad2b9e-d1c8-4dca-9379-15cd2e19b3ef/can-i-get-a-list-of-type-without-instantiating-using-mef?forum=MEFramework

    // https://github.com/dotnet/home/blob/master/projects/mef.md
    // https://www.codeproject.com/articles/376033/from-zero-to-proficient-with-mef
    
    

    // https://social.msdn.microsoft.com/Forums/en-US/eaad2b9e-d1c8-4dca-9379-15cd2e19b3ef/can-i-get-a-list-of-type-without-instantiating-using-mef?forum=MEFramework

    public static class PluginLoader
    {
        public static IPluginLoader Instance { get { return _PluginsFactoryWithResolver.Default; } }

        public static IEnumerable<Assembly> UsePlugins(this IPluginLoader factory, IEnumerable<PathString> paths)
        {
            return paths
                .Select(item => factory.UsePlugin(item))
                .Where(item => item != null);
        }
    }    



    sealed class _PluginsFactoryBasic : IPluginLoader
    {
        #region lifecycle

        private static readonly _PluginsFactoryBasic _Instace = new _PluginsFactoryBasic();

        public static _PluginsFactoryBasic Default => _Instace;

        static _PluginsFactoryBasic() { }

        #endregion        

        #region API

        public Assembly UsePlugin(PathString pluginAbsPath)
        {
            try { return AssemblyContext.LoadAssemblyFromFilePath(pluginAbsPath); } // this was originally using Assembly.Load();
            catch { return null; }
        }

        #endregion
    }



    sealed class _PluginsFactoryWithResolver : IPluginLoader
    {
        #region lifecycle

        private static readonly _PluginsFactoryWithResolver _Instace = new _PluginsFactoryWithResolver();

        public static _PluginsFactoryWithResolver Default => _Instace;

        private _PluginsFactoryWithResolver()
        {
            AssemblyContext.SetAssemblyResolver(_AssemblyResolve);
        }

        static _PluginsFactoryWithResolver() { }

        #endregion

        #region data

        private readonly Object _Lock = new object();

        private readonly HashSet<PathString> _ProbeDirectories = new HashSet<PathString>();

        // list of assemblies the framework was unable to load and were handled here
        private readonly Dictionary<String, PathString> _ResolvedAssembles = new Dictionary<string, PathString>();

        #endregion

        #region API        

        public Assembly UsePlugin(PathString pluginAbsPath)
        {
            try
            {
                if (!pluginAbsPath.IsValidAbsoluteFilePath) return null;
                if (!pluginAbsPath.FileExists) return null;

                lock(_Lock)
                {
                    _ProbeDirectories.Add(pluginAbsPath.DirectoryPath);
                }

                return AssemblyContext.LoadAssemblyFromFilePath(pluginAbsPath);
            }
            catch { return null; }
        }       

        private Assembly _AssemblyResolve(AssemblyName aname)
        {
            // https://msdn.microsoft.com/en-us/library/ff527268.aspx

            // this can be called by any thread

            string dllName = aname.Name + ".dll";

            PathString[] probeDirs = null;

            lock (_Lock)
            {
                _ResolvedAssembles[dllName] = PathString.Empty;
                probeDirs = _ProbeDirectories.ToArray();
            }            

            foreach(var probeDir in probeDirs)
            {
                var fullPath = System.IO.Path.Combine(probeDir, dllName);

                if (!System.IO.File.Exists(fullPath)) continue;

                try
                {
                    var a = AssemblyContext.LoadAssemblyFromFilePath(fullPath);
                    if (a != null)
                    {
                        System.Diagnostics.Debug.WriteLine("LOADED ASSEMBLY: " + fullPath);

                        lock (_Lock) { _ResolvedAssembles[aname.Name] = new PathString(fullPath); }
                        return a;
                    }
                }
                catch { }
            }

            System.Diagnostics.Debug.WriteLine("FAILED LOADING ASSEMBLY: " + dllName);

            return null;                
        }

        #endregion
    }    
}


