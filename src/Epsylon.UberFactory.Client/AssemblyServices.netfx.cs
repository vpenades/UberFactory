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

            _UnmanagedAssemblyServices.SetDllDirectory(finfo.Directory.FullName);

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
        private static readonly HashSet<string> _CurrentSearchPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms682586(v=vs.85).aspx
        // http://www.pinvoke.net/default.aspx/kernel32.setdlldirectory

        // https://github.com/MonoGame/MonoGame/blob/d28cd31ec3c5b0cfc1fc57210f6eb368dd47fb10/MonoGame.Framework/Utilities/CurrentPlatform.cs#L99

        [DllImport("kernel32.dll",EntryPoint = "SetDllDirectory", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool _SetDllDirectory(string lpPathName);

        public static bool SetDllDirectory(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName)) return false;
            if (!System.IO.Path.IsPathRooted(directoryName)) return false;            

            return _SetDllDirectory(directoryName);
        }
    }
}
