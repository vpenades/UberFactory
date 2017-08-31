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
        #region strings

        public static Guid ToGuidSafe(this string value)
        {
            if (value == null) return Guid.Empty;

            Guid r = Guid.Empty; return Guid.TryParse(value, out r) ? r : Guid.Empty;
        }

        public static String ToTitleCase(this string value)
        {
            // return value == null ? null : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);

            if (value == null) return null;

            if (value.Length == 1) return value.ToUpper();

            return value.Substring(0, 1).ToUpper() + value.Substring(1).ToLower();
        }

        public static string ToFriendlySystemPath(this Uri uri)
        {
            if (uri == null) return null;

            var path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.LocalPath;

            path = Uri.UnescapeDataString(path).Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

            return path;
        }

        #endregion

        #region math

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNan(this Single val) { return Single.IsNaN(val); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNan(this Double val) { return Double.IsNaN(val); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReal(this Single val) { return !Single.IsNaN(val) && !Single.IsInfinity(val); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReal(this Double val) { return !Double.IsNaN(val) && !Double.IsInfinity(val); }





        private const Single _SingleToDegrees = (Single)(180 / System.Math.PI);
        private const Single _SingleToRadians = (Single)(System.Math.PI / 180);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single ToDegrees(this Single radians) { return radians * _SingleToDegrees; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single ToRadians(this Single degrees) { return degrees * _SingleToRadians; }

        private const Double _DoubleToDegrees = (Double)(180 / System.Math.PI);
        private const Double _DoubleToRadians = (Double)(System.Math.PI / 180);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double ToDegrees(this Double radians) { return radians * _SingleToDegrees; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double ToRadians(this Double degrees) { return degrees * _SingleToRadians; }





        public static T Clamp<T>(this T v, T min, T max) where T:IConvertible , IComparable, IComparable<T>
        {
            if (min != null && v.CompareTo(min) < 0) v = min;
            if (max != null && v.CompareTo(max) > 0) v = max;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single Saturate(this Single v) { return v.Clamp(0, 1); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double Saturate(this Double v) { return v.Clamp(0, 1); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal Saturate(this Decimal v) { return v.Clamp(0, 1); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single Lerp(this Single a, Single b, Single factor) { return a * (1 - factor) + b * factor; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double Lerp(this Double a, Double b, Double factor) { return a * (1 - factor) + b * factor; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal Lerp(this Decimal a, Decimal b, Decimal factor) { return a * (1 - factor) + b * factor; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 ClampIndex<T>(this Int32 index, T[] array) { return index.Clamp(0, array.Length - 1); }

        #endregion

        #region linq

        public static int IndexOf<T>(this IEnumerable<T> collection, Predicate<T> condition)
        {
            int idx = -1;

            foreach(var item in collection)
            {
                ++idx;
                if (condition(item)) return idx;
            }

            return -1;
        }

        public static IEnumerable<T> ExceptNulls<T>(this IEnumerable<T> collection) where T : class { return collection.Where(item => item != null); }


        // GetValueOrDefault is added for the three Dict, IDict and IReadOnlyDict so it is supported for the interfaces, and prevents Dict to be ambiguous

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            return ((IReadOnlyDictionary<TKey, TValue>)dict).GetValueOrDefault(key);
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict.TryGetValue(key, out TValue val)) return val;

            return default(TValue);
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict.TryGetValue(key, out TValue val)) return val;

            return default(TValue);
        }

        public static IEnumerable<T> OfTypes<T>(this IEnumerable<T> collection, params Type[] types)
        {
            var tt = typeof(T).GetTypeInfo();

            return collection.Where(item => item != null && types.Any(t => t.GetTypeInfo().IsAssignableFrom(tt)));
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
        public static bool IsFramework(this System.Reflection.AssemblyName name)
        {
            if (name == null) return false;

            var pkey = System.Convert.ToBase64String(name.GetPublicKeyToken());

            if (pkey == "t3pcVhk04Ik=") return true; // system DLLs
            if (pkey == "Mb84Vq02TjU=") return true; // WPF DLLs

            return false;
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

        public static Type GetAssignType(this MemberInfo xinfo)
        {
            // this method returns the type to pass for assignement:
            // instance.SomeProperty = value;
            // instance.SomeMethod(value);
            // instance.SomeField = value;

            if (xinfo is PropertyInfo) return ((PropertyInfo)xinfo).PropertyType;
            if (xinfo is FieldInfo) return ((FieldInfo)xinfo).FieldType;
            if (xinfo is MethodInfo) return ((MethodInfo)xinfo).GetParameters()[0].ParameterType;

            throw new NotImplementedException();
        }

        public static Type GetEvaluateType(this MemberInfo xinfo)
        {
            // this method returns the type to pass for assignement:
            // value = instance.SomeProperty;
            // value = instance.SomeMethod();
            // value = instance.SomeField;

            if (xinfo is PropertyInfo) return ((PropertyInfo)xinfo).PropertyType;
            if (xinfo is FieldInfo) return ((FieldInfo)xinfo).FieldType;
            if (xinfo is MethodInfo) return ((MethodInfo)xinfo).ReturnType;

            throw new NotImplementedException();
        }

        public static SDK.InputPropertyAttribute GetInputDescAttribute(this MemberInfo xinfo)
        {
            return xinfo
                .GetCustomAttributes<SDK.InputPropertyAttribute>(true)
                .LastOrDefault();
        }

        #endregion

        #region ContentFilter extensions        

        public static MemberInfo TryGetReflectedMember(this SDK.ContentObject nodeInstance, String key)
        {
            var pinfo = nodeInstance
                    .GetType()
                    .GetTypeInfo()
                    .FlattenedProperties()
                    .FirstOrDefault(item => item.Name == key);

            if (pinfo != null) return pinfo;

            var minfo = nodeInstance
                .GetType()
                .GetTypeInfo()
                .FlattenedMethods()
                .FirstOrDefault(item => item.Name == key);

            if (minfo != null) return minfo;

            return null;
        }

        public static void TryAssign(this SDK.ContentObject nodeInstance, MemberInfo xinfo, Object value)
        {
            if (xinfo is PropertyInfo pinfo)
            {
                value = pinfo
                    .PropertyType
                    .GetTypeInfo()
                    .ConvertBindableValue(value);

                pinfo.SetValue(nodeInstance, value);
                return;
            }

            if (xinfo is FieldInfo finfo)
            {
                value = finfo
                    .FieldType
                    .GetTypeInfo()
                    .ConvertBindableValue(value);

                finfo.SetValue(nodeInstance, value);
                return;
            }

            if (xinfo is MethodInfo minfo)
            {
                value = minfo
                    .GetParameters()[0]
                    .ParameterType
                    .GetTypeInfo()
                    .ConvertBindableValue(value);

                minfo.Invoke(nodeInstance, new Object[] { value });
                return;
            }
        }

        public static Object TryEvaluate(this SDK.ContentObject nodeInstance, String key)
        {
            var xinfo = nodeInstance.TryGetReflectedMember(key);

            if (xinfo is PropertyInfo) return ((PropertyInfo)xinfo).GetValue(nodeInstance);
            if (xinfo is FieldInfo) return ((FieldInfo)xinfo).GetValue(nodeInstance);
            if (xinfo is MethodInfo) return ((MethodInfo)xinfo).Invoke(nodeInstance, null);

            return null;
        }

        public static T GetValue<T>(this SDK.MetaDataKeyAttribute attrib, SDK.ContentObject instance, object defval)
        {
            object value = defval;

            if (attrib is SDK.ContentMetaDataAttribute)
            {
                value = ((SDK.ContentMetaDataAttribute)attrib).Value;
            }

            if (attrib is SDK.InputMetaDataAttribute)
            {
                value = ((SDK.InputMetaDataAttribute)attrib).Value;
            }

            if (attrib is SDK.InputMetaDataEvaluateAttribute)
            {
                if (instance == null) throw new ArgumentNullException(nameof(instance));

                var xattrib = attrib as SDK.InputMetaDataEvaluateAttribute;                
                if (xattrib.SharedType != null) instance = instance.GetSharedSettings(xattrib.SharedType);

                var propName = xattrib.PropertyName;
                value = instance.TryEvaluate(propName);
            }

            return (T)typeof(T).GetTypeInfo().ConvertBindableValue(value);
        }

        #endregion

        #region value conversion

        public static IPropertyProvider AsReadOnly(this IPropertyProvider props) { return props == null ? null : new _ReadOnlyLayer(props); }
        

        public static Object ConvertBindableValue(this TypeInfo expectedType, Object value)
        {
            if (value == null) return expectedType.IsValueType ? Activator.CreateInstance(expectedType.AsType()) : null;                

            var valueType = value.GetType().GetTypeInfo();

            if (expectedType.IsAssignableFrom(valueType)) return value;            

            // arrays

            if (valueType.IsArray && expectedType.IsArray && valueType.GetElementType() != expectedType.GetElementType())
            {
                var oldArray = (Array)value;
                var newType = expectedType.GetElementType().GetTypeInfo();

                var newArray = Array.CreateInstance(newType.AsType(), oldArray.Length);

                for (int i = 0; i < newArray.Length; ++i)
                {
                    newArray.SetValue(newType.ConvertBindableValue(oldArray.GetValue(i)), i);
                }

                return newArray;
            }

            // default conversion

            if (typeof(IConvertible).GetTypeInfo().IsAssignableFrom(expectedType) && value is IConvertible)
            {
                return Convert.ChangeType(value, expectedType.AsType());
            }

            // special cases

            if (expectedType.AsType() == typeof(PathString) && value is String) return new PathString((String)value);

            if (expectedType.AsType() == typeof(System.IO.FileInfo) && value is String) return new System.IO.FileInfo((String)value);
            if (expectedType.AsType() == typeof(System.IO.DirectoryInfo) && value is String) return new System.IO.DirectoryInfo((String)value);            

            if (expectedType.AsType() == typeof(String) && value is PathString) return (String)value;

            if (expectedType.AsType() == typeof(String) && value is System.IO.FileInfo) return ((System.IO.FileInfo)value).FullName;
            if (expectedType.AsType() == typeof(String) && value is System.IO.DirectoryInfo) return ((System.IO.DirectoryInfo)value).FullName;            

            throw new NotImplementedException();
        }                

        public static TValue ConvertToValue<TValue>(this String value) where TValue : IConvertible
        {
            return value == null ? default(TValue) : (TValue)Convert.ChangeType(value, typeof(TValue), System.Globalization.CultureInfo.InvariantCulture);
        }

        public static String ConvertToString<TValue>(this TValue value) where TValue : IConvertible
        {
            return value == null ? null : (String)Convert.ChangeType(value, typeof(String), System.Globalization.CultureInfo.InvariantCulture);
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


    public static class _CoreExtensions
    {
        #region evaluation

        public static IEnumerable<Assembly> UsePlugins(this Evaluation.IPluginLoader factory, IEnumerable<PathString> paths)
        {
            return paths
                .Select(item => factory.UsePlugin(item))
                .Where(item => item != null);
        }

        #endregion
    }
}
