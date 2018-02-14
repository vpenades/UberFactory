using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Client
{
    using ASSEMBLYRESOLVEFUNC = Func<AssemblyName, Assembly>;

    /// <summary>
    /// Utility class to load assemblies dynamically, as in plugin based systems.
    /// </summary>
    /// <remarks>
    /// Dynamically Loading assemblies is currently supported only by these two platforms:
    /// - Net.FX   uses System.AppDomain
    /// - Net.Core uses System.Runtime.Loader.AssemblyLoadContext which requires System.Runtime.Loader NuGet Package
    /// </remarks>
    public static partial class AssemblyServices
    {
        #region data

        private static readonly string _EntryAssemblyDirectory = GetEntryAssembly()?.GetDirectory();

        private static ASSEMBLYRESOLVEFUNC _AssemblySolver = null;

        #endregion

        #region properties

        public static string EntryAssemblyDirectory => _EntryAssemblyDirectory;

        #endregion

        #region API

        public static System.Diagnostics.FileVersionInfo LoadVersionInfo(string absFilePath)
        {
            return System.Diagnostics.FileVersionInfo.GetVersionInfo(absFilePath);
        }

        #endregion
    }

}
