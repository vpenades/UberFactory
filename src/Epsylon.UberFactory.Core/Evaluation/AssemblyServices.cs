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
    public static partial class AssemblyServices
    {
        private static readonly string _EntryAssemblyDirectory = GetEntryAssembly()?.GetDirectory();

        private static ASSEMBLYRESOLVEFUNC _AssemblySolver = null;

        public static string EntryAssemblyDirectory => _EntryAssemblyDirectory;
    }

}
