using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
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

        public IEnumerable<Factory.ContentObjectTypeInfo> SettingsTypes
        {
            get
            {
                var types = _GetExportedTypes<SDK.ContentObject>()
                    .Where(item => typeof(SDK.ContentObject).GetTypeInfo().IsAssignableFrom(item))    // Must derive from SDK.ContentObject
                    .Where(item => !typeof(SDK.ContentFilter).GetTypeInfo().IsAssignableFrom(item))   // Must NOT derive from SDK.ContentFilter
                    .Select(t => t.GetContentTypeInfo())
                    .ExceptNulls()
                    .OfType<Factory.ContentObjectTypeInfo>()
                    .ToArray();

                return types;
            }
        }

        public IEnumerable<Factory.ContentFilterTypeInfo> PluginTypes
        {
            get
            {
                var types = _GetExportedTypes<SDK.ContentFilter>()
                    .Select(t => t.GetContentTypeInfo())
                    .ExceptNulls()
                    .OfType<Factory.ContentFilterTypeInfo>()
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

        public SDK.ContentObject CreateInstance(string classId)
        {
            if (string.IsNullOrWhiteSpace(classId)) throw new ArgumentNullException(nameof(classId));

            var factory1 = PluginTypes.FirstOrDefault(item => item.SerializationKey == classId);
            if (factory1 != null) return factory1.CreateInstance();


            var factory2 = SettingsTypes.FirstOrDefault(item => item.SerializationKey == classId);
            if (factory2 != null) return factory2.CreateInstance();

            return new _UnknownNode();
        }

        #endregion
    }


    
}
