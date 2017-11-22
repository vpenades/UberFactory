using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static class Factory
    {
        /// <summary>
        /// Gets a specialised TypeInfo with extra information about a ContentFilter
        /// </summary>
        /// <param name="instance">Any object, but designed for <code>SDK.ContentFilter</code> derived objects</param>
        /// <returns>A <code>ContentFilterTypeInfo</code> instace for valid <code>SDK.ContentFilter</code>, and <code>UnknownTypeInfo</code> for everything else</returns>
        public static ContentBaseInfo GetContentInfo(this Object instance)
        {
            if (instance is Type) return GetContentInfo((Type)instance);

            return GetContentInfo(instance?.GetType());
        }

        /// <summary>
        /// Gets a specialised TypeInfo with extra information about a ContentFilter
        /// </summary>
        /// <param name="t">Any type, but designed for <code>SDK.ContentFilter</code> derived types</param>
        /// <returns>A <code>ContentFilterTypeInfo</code> instace for valid <code>SDK.ContentFilter</code>, and <code>UnknownTypeInfo</code> for everything else</returns>
        public static ContentBaseInfo GetContentInfo(this Type t)
        {
            if (t == null) return null;
            if (typeof(SDK.ContentFilter).GetTypeInfo().IsAssignableFrom(t)) return ContentFilterInfo.Create(t);
            return ContentObjectInfo.Create(t);            
        }


        public static Collection GetContentInfoCollection(this IEnumerable<Assembly> assemblies)
        {
            var c = new Collection();
            c.SetAssemblies(assemblies);
            return c;
        }


        public class Collection
        {
            #region data        

            private Assembly[] _Assemblies;
            private ContentObjectInfo[] _SettingsTypes;
            private ContentFilterInfo[] _PluginTypes;

            #endregion

            #region API        

            public void SetAssemblies(IEnumerable<Assembly> assemblies)
            {
                _Assemblies = assemblies
                    .ExceptNulls()
                    .Distinct()
                    .ToArray();

                _SettingsTypes = ContentObjectInfo.GetContentTypes(_Assemblies).ToArray();
                _PluginTypes = ContentFilterInfo.GetContentTypes(_Assemblies).ToArray();
            }

            public IReadOnlyList<ContentObjectInfo> SettingsTypes => _SettingsTypes;

            public IReadOnlyList<String> SettingsClassIds => SettingsTypes.Select(item => item.SerializationKey).Distinct().ToArray();

            public IReadOnlyList<ContentFilterInfo> PluginTypes => _PluginTypes;

            public SDK.ContentObject CreateInstance(string classId)
            {
                if (string.IsNullOrWhiteSpace(classId)) throw new ArgumentNullException(nameof(classId));

                var factory1 = _PluginTypes.FirstOrDefault(item => item.SerializationKey == classId);
                if (factory1 != null) return factory1.CreateInstance();

                var factory2 = _SettingsTypes.FirstOrDefault(item => item.SerializationKey == classId);
                if (factory2 != null) return factory2.CreateInstance();

                return new Evaluation._UnknownNode();
            }

            #endregion
        }

        /// <summary>
        /// Base class for type information for ContentFilters
        /// </summary>
        public abstract class ContentBaseInfo
        {
            #region lifecycle

            public ContentBaseInfo(Type t) { _Type = t; }

            #endregion

            #region data

            protected readonly Type _Type;

            #endregion

            #region properties

            public abstract string SerializationKey { get; }

            public abstract string DisplayName { get; }

            public abstract string DisplayFormatName { get; }

            #endregion

            #region API            

            protected T GetMetaDataValue<T>(String key, T defval)
            {
                if (_Type == null) return defval;

                var attrib = _Type.GetTypeInfo().GetCustomAttributes(true)
                    .OfType<SDK.ContentMetaDataAttribute>()
                    .FirstOrDefault(item => item.Key == key);

                if (attrib == null) return defval;

                return attrib.GetValue<T>(null, defval);
            }

            protected static IEnumerable<Type> GetDefinedTypes<T>(IEnumerable<Assembly> assemblies)
            {
                if (assemblies == null) yield break;

                var ti = typeof(T).GetTypeInfo();

                foreach (var a in assemblies)
                {
                    foreach (var t in a.DefinedTypes)
                    {
                        if (t.GetTypeInfo().IsAbstract) continue;
                        if (!t.GetTypeInfo().IsClass) continue;
                        if (t.IsNestedPrivate) continue;

                        if (ti.IsAssignableFrom(t.GetTypeInfo())) yield return t;
                    }
                }
            }

            #endregion            
        }

        /// <summary>
        /// Type information for unknown/missing/null content filter
        /// </summary>
        public sealed class UnknownInfo : ContentBaseInfo
        {
            #region lifecycle

            internal UnknownInfo() : base(typeof(Object)) { }

            #endregion

            #region API

            public override string SerializationKey => throw new NotImplementedException();

            public override string DisplayName => "Failed to load!";

            public override string DisplayFormatName => "{0} = Failed to load!";            

            #endregion
        }

        /// <summary>
        /// Type information for ContentFilters
        /// </summary>
        [System.Diagnostics.DebuggerDisplay("{DisplayName}  {InputTypes} > {OutputType}")]
        public sealed class ContentFilterInfo : ContentBaseInfo
        {
            #region lifecycle

            public static ContentFilterInfo Create(SDK.ContentFilter node)
            {
                if (node == null) return null;
                
                return Create(node.GetType());
            }

            public static ContentFilterInfo Create(Type t)
            {
                if (t == null) return null;

                var tinfo = t.GetTypeInfo();

                if (tinfo.IsAbstract) return null;                

                if (!typeof(SDK.ContentFilter).GetTypeInfo().IsAssignableFrom(tinfo)) return null;

                if (_GetDeclarationAttribute(t) == null) return null;

                return new ContentFilterInfo(t);
            }

            private ContentFilterInfo(Type t) : base(t) { }

            #endregion

            #region properties

            public override string  SerializationKey    => _GetSerializationKey();

            public override string  DisplayName         => GetMetaDataValue<String>("Title", _Type.Name);

            public override string  DisplayFormatName   => GetMetaDataValue<String>("TitleFormat", DisplayName + " {0}");

            public Type             OutputType          => _GetGenericOutputArgument();

            #endregion

            #region API

            private Type _GetGenericOutputArgument()
            {
                var t = _Type;                

                while(t != null)
                {
                    if (t.IsConstructedGenericType)
                    {
                        if (t.GetGenericTypeDefinition() == typeof(SDK.ContentFilter<>))
                        {
                            if (t.GenericTypeArguments.Length > 0) return t.GenericTypeArguments[0];
                        }
                    }

                    t = t.GetTypeInfo().BaseType;
                }

                return null;                
            }
            
            private string _GetSerializationKey()
            {
                var attr = _Type
                        .GetTypeInfo()
                        .Assembly
                        .GetCustomAttributes<AssemblyMetadataAttribute>()
                        .FirstOrDefault(item => item.Key == "SerializationRoot");

                var root = attr != null ? attr.Value : string.Empty;

                return root + _GetDeclarationAttribute(_Type).SerializationKey;
            }

            public SDK.ContentFilter CreateInstance() { return SDK.Create(_Type) as SDK.ContentFilter; }

            private static SDK.ContentNodeAttribute _GetDeclarationAttribute(Type t)
            {
                return t.GetTypeInfo().GetCustomAttributes(true)
                    .OfType<SDK.ContentNodeAttribute>()
                    .FirstOrDefault();
            }

            public static IEnumerable<Factory.ContentFilterInfo> GetContentTypes(IEnumerable<Assembly> assemblies)
            {                
                var types = GetDefinedTypes<SDK.ContentFilter>(assemblies)
                    .Select(t => t.GetContentInfo())
                    .ExceptNulls()
                    .OfType<Factory.ContentFilterInfo>()
                    .ToArray();

                return types;                
            }

            #endregion
        }

        
        public sealed class ContentObjectInfo : ContentBaseInfo
        {
            #region lifecycle

            public static ContentObjectInfo Create(object anyInstance)
            {
                if (anyInstance == null) return null;

                return Create(anyInstance.GetType());
            }

            public static ContentObjectInfo Create(Type t)
            {
                if (t == null) return null;

                var tinfo = t.GetTypeInfo();

                if (tinfo.IsAbstract) return null;                

                if (_GetDeclarationAttribute(t) == null) return null;

                return new ContentObjectInfo(t);
            }

            private ContentObjectInfo(Type t) : base(t) { }

            #endregion

            #region properties

            public override string SerializationKey => _GetSerializationKey();

            public override string DisplayName => GetMetaDataValue<String>("Title", _Type.Name);

            public override string DisplayFormatName => GetMetaDataValue<String>("TitleFormat", DisplayName + " {0}");

            #endregion

            #region API

            private string _GetSerializationKey()
            {
                var attr = _Type
                        .GetTypeInfo()
                        .Assembly
                        .GetCustomAttributes<AssemblyMetadataAttribute>()
                        .FirstOrDefault(item => item.Key == "SerializationRoot");

                var root = attr != null ? attr.Value : string.Empty;

                return root + _GetDeclarationAttribute(_Type).SerializationKey;
            }

            public SDK.ContentObject CreateInstance() { return SDK.Create(_Type) as SDK.ContentObject; }

            private static SDK.ContentNodeAttribute _GetDeclarationAttribute(Type t)
            {
                return t.GetTypeInfo().GetCustomAttributes(true)
                    .OfType<SDK.ContentNodeAttribute>()
                    .FirstOrDefault();
            }

            public static IEnumerable<Factory.ContentObjectInfo> GetContentTypes(IEnumerable<Assembly> assemblies)
            {                
                var types = GetDefinedTypes<SDK.ContentObject>(assemblies)
                    .Where(item => typeof(SDK.ContentObject).GetTypeInfo().IsAssignableFrom(item))    // Must derive from SDK.ContentObject
                    .Where(item => !typeof(SDK.ContentFilter).GetTypeInfo().IsAssignableFrom(item))   // Must NOT derive from SDK.ContentFilter
                    .Select(t => t.GetContentInfo())
                    .ExceptNulls()
                    .OfType<Factory.ContentObjectInfo>()
                    .ToArray();

                return types;
                
            }

            #endregion
        }

    }
}
