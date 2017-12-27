using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    static class _InternalExtensions
    {
        #region linq        

        public static IEnumerable<T> ExceptNulls<T>(this IEnumerable<T> collection) where T : class { return collection.Where(item => item != null); }

        public static T Clamp<T>(this T v, T min, T max) where T : IConvertible, IComparable, IComparable<T>
        {
            if (min != null && v.CompareTo(min) < 0) v = min;
            if (max != null && v.CompareTo(max) > 0) v = max;

            return v;
        }

        #endregion

        #region assemblies

        /// <summary>
        /// checks if an assembly is part of the runtime framework
        /// </summary>
        /// <param name="assembly">Assembly to check</param>
        /// <returns>True if runtime framework, false otherwise</returns>
        public static bool IsFramework(this Assembly assembly)
        {
            return assembly == null ? false : assembly.GetName().IsFramework();
        }

        /// <summary>
        /// checks if an AssemblyName is part of the runtime framework
        /// </summary>
        /// <param name="name">AssemblyName to check</param>
        /// <returns>True if runtime framework, false otherwise</returns>
        public static bool IsFramework(this AssemblyName name)
        {
            if (name == null) return false;

            var pkey = System.Convert.ToBase64String(name.GetPublicKeyToken());

            if (pkey == "t3pcVhk04Ik=") return true; // system DLLs
            if (pkey == "Mb84Vq02TjU=") return true; // WPF DLLs

            return false;
        }

        /// <summary>
        /// Tells if the given processor architecture is compatible with the current runtime architecture
        /// </summary>
        /// <param name="architecture">architecture</param>
        /// <returns>true if compatible</returns>
        public static bool IsRuntimeCompatible(this ProcessorArchitecture architecture)
        {
            if (architecture == ProcessorArchitecture.None) return false;

            if (architecture == ProcessorArchitecture.X86 && IntPtr.Size != 4) return false;
            if (architecture == ProcessorArchitecture.Amd64 && IntPtr.Size != 8) return false;
            if (architecture == ProcessorArchitecture.IA64 && IntPtr.Size != 8) return false;

            if (architecture == ProcessorArchitecture.Arm) return false;

            return true;
        }

        public static bool CheckPluginCompatibility(this PathString pluginAbsPath)
        {
            try
            {
                if (!pluginAbsPath.IsValidAbsoluteFilePath) return false;
                if (!pluginAbsPath.FileExists) return false;

                var fvi = Client.AssemblyServices.LoadVersionInfo(pluginAbsPath);
                if (fvi == null) return false;
                var aname = Client.AssemblyServices.GetAssemblyName(pluginAbsPath);
                if (aname == null) return false;

                if (aname.IsFramework()) return false;
                if (!aname.ProcessorArchitecture.IsRuntimeCompatible()) return false;

                return true;
            }
            catch { return false; }
        }

        public static Assembly LoadCompatiblePlugin(this PathString pluginAbsPath)
        {
            if (!pluginAbsPath.CheckPluginCompatibility()) return null;

            try
            {
                // this was originally using Assembly.Load();
                var a = Client.AssemblyServices.LoadAssemblyFromFilePath(pluginAbsPath);

                if (a.IsFramework()) return null;

                a.DefinedTypes.ToArray(); // if not compatible, this throws ReflectionTypeLoadException

                return a;
            }
            catch (ReflectionTypeLoadException) { return null; }
        }

        #endregion

        #region basic reflection

        public static IEnumerable<PropertyInfo> FlattenedProperties(this TypeInfo tinfo)
        {
            if (tinfo == null) return Enumerable.Empty<PropertyInfo>();

            var props = tinfo.DeclaredProperties;

            if (tinfo.BaseType != null) props = tinfo.BaseType.GetTypeInfo().FlattenedProperties().Concat(props);

            return props;
        }

        public static IEnumerable<MethodInfo> FlattenedMethods(this TypeInfo tinfo)
        {
            if (tinfo == null) return Enumerable.Empty<MethodInfo>();

            var methods = tinfo.DeclaredMethods;

            if (tinfo.BaseType != null) methods = tinfo.BaseType.GetTypeInfo().FlattenedMethods().Concat(methods);

            return methods;
        }        
        
        #endregion        

        #region progress monitor

        public static IProgress<float> CreatePart(this IProgress<float> target, int part, int total)
        {
            if (target == null) return null;
            return new _ProgressPart(target, part, total);
        }

        private struct _ProgressPart : IProgress<float>
        {
            public _ProgressPart(IProgress<float> target, int part, int total)
            {
                _Target = target;                
                _Scale = 1.0f / (float)total;
                _Offset = (float)part / (float)total;
            }

            private readonly IProgress<float> _Target;            
            private readonly float _Scale;
            private readonly float _Offset;

            public void Report(float value)
            {
                _Target.Report( (value * _Scale + _Offset).Clamp(0,1) );
            }
        }

        #endregion        
    }    
}
