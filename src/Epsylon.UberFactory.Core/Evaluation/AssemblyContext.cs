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

        private static readonly Stack<AssemblyName> _ReentrancyCheck = new Stack<AssemblyName>();

        private static readonly string _EntryAssemblyDirectory = GetEntryAssembly()?.GetDirectory();

        #endregion

        #region core        

        private static Assembly _AssemblyResolve(AssemblyName aname, ASSEMBLYRESOLVEFUNC fallbackFunc)
        {
            // note: after Net 4.0 onwards, satellite assembly resources are also resolved here!

            lock (_LockObject)
            {
                if (_ReentrancyCheck.Contains(aname))
                {
                    System.Diagnostics.Debug.WriteLine("3. Failed to find " + aname.Name);
                    return null;
                }
                _ReentrancyCheck.Push(aname);
            }

            try
            {
                var dllPath = System.IO.Path.Combine(_EntryAssemblyDirectory, aname.Name + ".dll");
                return LoadAssemblyFromFilePath(dllPath);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("1. Can't find " + aname.Name + " in Application directory");
            }            

            try
            {
                return fallbackFunc(aname);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("2. Can't find " + aname.Name + " in fallback directories");
            }

            lock(_LockObject)
            {
                _ReentrancyCheck.Pop();
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
