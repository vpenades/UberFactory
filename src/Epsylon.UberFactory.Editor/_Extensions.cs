using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    internal static class _PrivateExtensions
    {
        #region string

        public static string EnsureNotNull(this string value) { return value ?? string.Empty; }

        public static String ToTitleCase(this string value)
        {
            return value == null ? null : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
        }

        public static bool ContainsWhiteSpaces(this string text)
        {
            return text.Any(item => char.IsWhiteSpace(item));
        }

        public static bool ContainsAny(this string text, params string[] values)
        {
            foreach (var value in values)
            {
                if (text.Contains(value)) return true;
            }

            return false;
        }

        public static String Wrap(this string text, string wrapper) { return wrapper + text + wrapper; }

        public static String Wrap(this string text, char wrapper) { return wrapper + text + wrapper; }        

        public static void TryOpenContainingFolder(this PathString path)
        {
            try
            {
                if (path.IsValidFilePath && path.FileExists)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{path.ToString()}\"");
                    return;
                }

                if (path.IsValidDirectoryPath && path.DirectoryExists) System.Diagnostics.Process.Start(path);
            }
            catch { }
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





        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Byte Clamp(this Byte val, Byte min, Byte max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SByte Clamp(this SByte val, SByte min, SByte max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16 Clamp(this UInt16 val, UInt16 min, UInt16 max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16 Clamp(this Int16 val, Int16 min, Int16 max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Clamp(this UInt32 val, UInt32 min, UInt32 max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 Clamp(this Int32 val, Int32 min, Int32 max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64 Clamp(this UInt64 val, UInt64 min, UInt64 max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64 Clamp(this Int64 val, Int64 min, Int64 max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single Clamp(this Single val, Single min, Single max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double Clamp(this Double val, Double min, Double max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal Clamp(this Decimal val, Decimal min, Decimal max)
        {
            System.Diagnostics.Debug.Assert(min <= max);
            return val > max ? max : (val < min ? min : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single Saturate(this Single v) { return v.Clamp(0, 1); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double Saturate(this Double v) { return v.Clamp(0, 1); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal Saturate(this Decimal v) { return v.Clamp(0, 1); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single Lerp(this Single a, Single b, Single factor) { return a * (1 - factor) + (b * factor); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double Lerp(this Double a, Double b, Double factor) { return a * (1 - factor) + (b * factor); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal Lerp(this Decimal a, Decimal b, Decimal factor) { return a * (1 - factor) + (b * factor); }

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

        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict.TryGetValue(key, out TValue val)) return val;

            return default(TValue);
        }

        public static bool ContainsAny<T>(this IEnumerable<T> collection, params T[] values)
        {
            foreach(var value in values)
            {
                if (collection.Contains(value)) return true;
            }

            return false;
        }

        #endregion

        #region assemblies

        public static Version Version(this Assembly assembly) { return assembly == null ? new Version() : assembly.GetName().Version; }        

        public static string InfoCompany(this Assembly assembly) { return assembly.GetCustomAttributes<AssemblyCompanyAttribute>().FirstOrDefault()?.Company; }

        public static string InfoProductName(this Assembly assembly) { return assembly.GetCustomAttributes<AssemblyProductAttribute>().FirstOrDefault()?.Product; }

        public static string InformationalVersion(this Assembly assembly) { return assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion; }

        public static string InfoCopyright(this Assembly assembly) { return assembly.GetCustomAttributes<AssemblyCopyrightAttribute>().FirstOrDefault()?.Copyright; }

        public static string DisplayTitle(this Assembly assembly) { return assembly == null ? null : assembly.InfoProductName() + " " + assembly.Version().ToString(); }

        public static string GetDisplayTitle(this Assembly assembly, bool displayCompany, bool displayVersion, string currentDocument)
        {
            if (assembly == null) return null;
            var title = assembly.InfoProductName();

            if (displayCompany) title = assembly.InfoCompany() + " " + title;
            if (displayVersion) title = title + " " + assembly.InformationalVersion();

            if (!string.IsNullOrWhiteSpace(currentDocument)) title = currentDocument.Trim() + " - " + title;

            return title;
        }

        public static string GetMetadata(this Assembly assembly, string key)
        {
            var attributes = Attribute.GetCustomAttributes(assembly, typeof(AssemblyMetadataAttribute), true);
            if (attributes == null) return null;

            return attributes.OfType<AssemblyMetadataAttribute>().FirstOrDefault(item => item.Key == key)?.Value;
        }
        
        public static bool IsLoaded(this System.Diagnostics.FileVersionInfo fvinfo)
        {
            return AppDomain
                    .CurrentDomain
                    .GetAssemblies()
                    .Where(a => !a.IsDynamic) // this is required to prevent crashes if there's a dynamically generated assebly loaded                    
                    .Any(a => string.Equals(a.Location, fvinfo.FileName, StringComparison.InvariantCultureIgnoreCase));
        }

        public static PathString Location(this System.Diagnostics.FileVersionInfo fvinfo) { return new PathString(fvinfo.FileName); }

        #endregion        
    }
}
