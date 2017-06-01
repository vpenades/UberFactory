using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{

    public interface IPluginLoader
    {
        Assembly UsePlugin(PathString pluginAbsPath);
    }
    
    public class PluginManager
    {
        #region data        

        private Assembly[] _Assemblies;        

        #endregion

        #region API        

        public void SetAssemblies(IEnumerable<Assembly> assemblies)
        {
            _Assemblies = assemblies.ToArray();
        }

        public IEnumerable<Factory.ContentFilterTypeInfo> PluginTypes
        {
            get
            {
                var types = _GetExportedTypes<SDK.ContentFilter>()
                    .Select(t => Factory.GetFilterTypeInfo(t))
                    .ExceptNulls()
                    .OfType<Factory.ContentFilterTypeInfo>()
                    .ToArray();

                return types;
            }
        }

        public IEnumerable<Factory.GlobalSettingsTypeInfo> SettingsTypes
        {
            get
            {
                var types = _GetExportedTypes<SDK.ContentObject>()
                    .Where(item => !typeof(SDK.ContentFilter).IsAssignableFrom(item) )
                    .Select(t => Factory.GetFilterTypeInfo(t))
                    .ExceptNulls()
                    .OfType<Factory.GlobalSettingsTypeInfo>()
                    .ToArray();

                return types;
            }
        }

        private IEnumerable<Type> _GetExportedTypes<T>()
        {
            if (_Assemblies == null) yield break;

            var ti = typeof(T).GetTypeInfo();

            foreach(var a in _Assemblies)
            {
                foreach(var t in a.ExportedTypes)
                {
                    if (t.GetTypeInfo().IsAbstract) continue;                    
                    if (!t.GetTypeInfo().IsClass) continue;

                    if (ti.IsAssignableFrom(t.GetTypeInfo())) yield return t;
                }
            }
        }


        public SDK.ContentFilter CreateContentFilterInstance(string classId, BuildContext bcontext)
        {
            if (string.IsNullOrWhiteSpace(classId)) throw new ArgumentNullException(nameof(classId));

            var factory = PluginTypes.FirstOrDefault(item => item.SerializationKey == classId);
            if (factory == null) return new _UnknownNode();

            return factory.CreateInstance(bcontext);
        }

        public SDK.ContentObject CreateGlobalSettingsInstance(string classId, BuildContext bcontext)
        {
            if (string.IsNullOrWhiteSpace(classId)) throw new ArgumentNullException(nameof(classId));

            var factory = SettingsTypes.FirstOrDefault(item => item.SerializationKey == classId);
            if (factory == null) return new _UnknownNode();

            return factory.CreateInstance(bcontext);
        }

        #endregion
    }


    
}
