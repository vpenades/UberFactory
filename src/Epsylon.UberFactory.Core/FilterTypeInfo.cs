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
        public static ContentBaseTypeInfo GetContentTypeInfo(this Object instance)
        {
            if (instance is Type) return GetContentTypeInfo((Type)instance);

            return GetContentTypeInfo(instance == null ? null : instance.GetType());
        }

        /// <summary>
        /// Gets a specialised TypeInfo with extra information about a ContentFilter
        /// </summary>
        /// <param name="t">Any type, but designed for <code>SDK.ContentFilter</code> derived types</param>
        /// <returns>A <code>ContentFilterTypeInfo</code> instace for valid <code>SDK.ContentFilter</code>, and <code>UnknownTypeInfo</code> for everything else</returns>
        public static ContentBaseTypeInfo GetContentTypeInfo(this Type t)
        {
            if (typeof(SDK.ContentFilter).IsAssignableFrom(t)) return ContentFilterTypeInfo.Create(t);
            return GlobalSettingsTypeInfo.Create(t);
            
        }

        /// <summary>
        /// Base class for type information for ContentFilters
        /// </summary>
        public abstract class ContentBaseTypeInfo
        {
            #region lifecycle

            public ContentBaseTypeInfo(Type t) { _Type = t; }

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
                    .OfType<SDK.ContentFilterMetaDataAttribute>()
                    .FirstOrDefault(item => item.Key == key);

                if (attrib == null) return defval;

                return attrib.GetValue<T>(null, defval);
            }

            #endregion
        }

        /// <summary>
        /// Type information for unknown/missing/null content filter
        /// </summary>
        public sealed class UnknownTypeInfo : ContentBaseTypeInfo
        {
            #region lifecycle

            internal UnknownTypeInfo() : base(typeof(Object)) { }

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
        public sealed class ContentFilterTypeInfo : ContentBaseTypeInfo
        {
            #region lifecycle

            public static ContentFilterTypeInfo Create(SDK.ContentFilter node)
            {
                if (node == null) return null;
                
                return Create(node.GetType());
            }

            public static ContentFilterTypeInfo Create(Type t)
            {
                if (t == null) return null;

                var tinfo = t.GetTypeInfo();

                if (tinfo.IsAbstract) return null;                

                if (!typeof(SDK.ContentFilter).GetTypeInfo().IsAssignableFrom(tinfo)) return null;

                if (_GetDeclarationAttribute(t) == null) return null;

                return new ContentFilterTypeInfo(t);
            }

            private ContentFilterTypeInfo(Type t) : base(t) { }

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

            public SDK.ContentFilter CreateInstance(BuildContext bsettings) { return SDK.Create(_Type, bsettings) as SDK.ContentFilter; }

            private static SDK.ContentFilterAttribute _GetDeclarationAttribute(Type t)
            {
                return t.GetTypeInfo().GetCustomAttributes(true)
                    .OfType<SDK.ContentFilterAttribute>()
                    .FirstOrDefault();
            }

            #endregion
        }

        
        public sealed class GlobalSettingsTypeInfo : ContentBaseTypeInfo
        {
            #region lifecycle

            public static GlobalSettingsTypeInfo Create(object anyInstance)
            {
                if (anyInstance == null) return null;

                return Create(anyInstance.GetType());
            }

            public static GlobalSettingsTypeInfo Create(Type t)
            {
                if (t == null) return null;

                var tinfo = t.GetTypeInfo();

                if (tinfo.IsAbstract) return null;                

                if (_GetDeclarationAttribute(t) == null) return null;

                return new GlobalSettingsTypeInfo(t);
            }

            private GlobalSettingsTypeInfo(Type t) : base(t) { }

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

            public SDK.ContentObject CreateInstance(BuildContext bsettings) { return SDK.Create(_Type, bsettings) as SDK.ContentObject; }

            private static SDK.GlobalSettingsAttribute _GetDeclarationAttribute(Type t)
            {
                return t.GetTypeInfo().GetCustomAttributes(true)
                    .OfType<SDK.GlobalSettingsAttribute>()
                    .FirstOrDefault();
            }

            #endregion
        }

    }
}
