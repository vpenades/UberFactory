using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
{
    using ASSEMBLYRESOLVEFUNC = Func<AssemblyName, Assembly>;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Assembly Loading can only be done with two APIs:
    /// Net.Framework uses System.AppDomain
    /// Net.Core uses System.Runtime.Loader.AssemblyLoadContext which requires System.Runtime.Loader NuGet Package
    /// </remarks>
    public static partial class AssemblyContext
    {
        #region data

        private static readonly Object _LockObject = new object();
        private static ASSEMBLYRESOLVEFUNC _AssemblySolver = null;

        private static readonly string _EntryAssemblyDirectory = GetEntryAssembly()?.GetDirectory();

        #endregion

        #region core

        private static Assembly _AssemblyResolve(AssemblyName aname, ASSEMBLYRESOLVEFUNC fallbackFunc)
        {
            // note: after Net 4.0 onwards, satellite assembly resources are also resolved here!

            try
            {
                var dllPath = System.IO.Path.Combine(_EntryAssemblyDirectory, aname.Name + ".dll");
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

        private static Assembly _AssemblyResolve(AssemblyName aname)
        {
            lock (_LockObject)
            {
                if (_AssemblySolver == null) return null;
                return _AssemblySolver(aname);
            }
        }

        #endregion

    }
}
