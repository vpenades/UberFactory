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

        public IEnumerable<Factory.ContentBaseTypeInfo> PluginTypes
        {
            get
            {
                var types = _GetExportedTypes<SDK.ContentFilter>()
                    .Select(t => Factory.GetContentFilterInfo(t))
                    .ExceptNulls()                    
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
                    if (ti.IsAssignableFrom(t.GetTypeInfo())) yield return t;
                }
            }
        }


        public SDK.ContentFilter CreateNodeInstance(string classId, BuildContext bcontext)
        {
            if (string.IsNullOrWhiteSpace(classId)) throw new ArgumentNullException(nameof(classId));

            var factory = PluginTypes.FirstOrDefault(item => item.SerializationKey == classId);
            if (factory == null) return new _UnknownNode();

            return factory.CreateInstance(bcontext);
        }

        #endregion
    }


    
}
