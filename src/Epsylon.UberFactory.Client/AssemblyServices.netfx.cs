using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Client
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
            // System.Diagnostics.Debug.Assert(!absoluteFilePath.Contains("\\..\\"));

            // Note that MEF uses Assembly.Load(AssemblyName.GetAssemblyName(absPath));
            // http://stackoverflow.com/questions/1477843/difference-between-loadfile-and-loadfrom-with-net-assemblies/1477900#1477900
            // Conclusion: better uses LoadFrom always

            var finfo = new System.IO.FileInfo(absoluteFilePath);

            if (!finfo.Exists) return null;

            _UnmanagedAssemblyServices.AddDllSearchDirectory(finfo.Directory.FullName);

            return Assembly.LoadFrom(finfo.FullName);
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


    /// <summary>
    /// Kernel32 unmanaged Assembly load utilities
    /// </summary>
    /// <remarks>
    /// Unmanaged DLLs are not resolved with standard AppDomain.CurrentDomain.AssemblyResolve,
    /// but we can add a search path so the unmanaged DLLs will be searched in these paths.
    /// Whenever we dynamically load a MANAGED dll, we tell the kernel to use its directory as a search path for UNMANAGED DLLs
    /// </remarks>
    static class _UnmanagedAssemblyServices
    {
        // https://stackoverflow.com/questions/21710982/how-to-adjust-path-for-dynamically-loaded-native-dlls
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms682586(v=vs.85).aspx
        // https://github.com/MonoGame/MonoGame/blob/d28cd31ec3c5b0cfc1fc57210f6eb368dd47fb10/MonoGame.Framework/Utilities/CurrentPlatform.cs#L99

        #region lifecycle

        static _UnmanagedAssemblyServices()
        {
            // enables using AddDllDirectory & RemoveDllDirectory
            _SetDefaultDllDirectories
                (
                LOAD_LIBRARY_SEARCH_APPLICATION_DIR
                |
                LOAD_LIBRARY_SEARCH_USER_DIRS
                |
                LOAD_LIBRARY_SEARCH_SYSTEM32
                |
                LOAD_LIBRARY_SEARCH_DEFAULT_DIRS
                );
        }

        #endregion

        #region data

        private static readonly HashSet<string> _CurrentSearchPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<IntPtr> _DirectoryHandles = new HashSet<IntPtr>();

        #endregion

        #region NATIVE API

        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/hh310515(v=vs.85).aspx"/>
        /// <seealso cref="http://www.pinvoke.net/default.aspx/kernel32.SetDefaultDllDirectories"/>
        [DllImport("kernel32", EntryPoint = "SetDefaultDllDirectories", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _SetDefaultDllDirectories(uint directoryFlags);
        const uint LOAD_LIBRARY_SEARCH_APPLICATION_DIR  = 0x00000200;
        const uint LOAD_LIBRARY_SEARCH_USER_DIRS        = 0x00000400;
        const uint LOAD_LIBRARY_SEARCH_SYSTEM32         = 0x00000800;        
        const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS     = 0x00001000;

        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms686203(v=vs.85).aspx"/>
        /// <seealso cref="http://www.pinvoke.net/default.aspx/kernel32.setdlldirectory"/>
        [DllImport("kernel32.dll",EntryPoint = "SetDllDirectory", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _SetDllDirectory(string lpPathName);
        
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/hh310513(v=vs.85).aspx"/>
        /// <seealso cref="http://source.roslyn.io/#Microsoft.CodeAnalysis.Remote.ServiceHub/Services/RemoteHostService.cs,278"/>
        [DllImport("kernel32.dll", EntryPoint = "AddDllDirectory", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr _AddDllDirectory(string directory);

        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/hh310514(v=vs.85).aspx"/>
        [DllImport("kernel32.dll", EntryPoint = "RemoveDllDirectory", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _RemoveDllDirectory(IntPtr handle);

        #endregion

        #region API

        public static bool AddDllSearchDirectory(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName)) return false;
            if (!System.IO.Path.IsPathRooted(directoryName)) return false;

            if (_CurrentSearchPaths.Contains(directoryName)) return true;
            _CurrentSearchPaths.Add(directoryName);

            var handle = _AddDllDirectory(directoryName);

            _DirectoryHandles.Add(handle);            

            return true;
        }

        public static void ClearSearchDirectories()
        {
            foreach(var handle in _DirectoryHandles)
            {
                _RemoveDllDirectory(handle);
            }

            _DirectoryHandles.Clear();
            _CurrentSearchPaths.Clear();
        }

        #endregion
    }
}
