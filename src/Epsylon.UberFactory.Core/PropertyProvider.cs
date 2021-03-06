﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    // this interfaces and classes are implemented and created by the serializable DOM objects
    // so the bindings can read and write property values from and to the DOM objects.
    
    public interface IPropertyProvider
    {
        bool Contains(String serializationKey);

        void Clear(String serializationKey);

        /// <summary>
        /// Returns the underlaying value or defval if there's no parent configuration.
        /// </summary>
        /// <param name="serializationKey">the Property Key</param>
        /// <param name="defval">The default value if there's no parent configuration</param>
        /// <returns>A String value</returns>
        String GetDefaultValue(String serializationKey, String defval);

        String GetValue(String serializationKey, String defval);
        bool SetValue(String serializationKey, String value);

        String[] GetArray(String serializationKey,params String[] defval);
        bool SetArray(String serializationKey,params String[] array);
    }
    
    public static class IPropertyProviderExtensions
    {
        private static Guid _ParseGuidReference(string value)
        {
            if (value == null) return Guid.Empty;

            return Guid.TryParse(value, out var r) ? r : Guid.Empty;
        }

        private static string _ToNodeReference(Guid id)
        {
            return id.ToString();
        }        

        public static Guid[] GetReferenceIds(this IPropertyProvider properties, string key, params string[] defval)
        {
            var values = properties.GetArray(key, defval);
            if (values == null) values = new string[0];

            return values
                .Select(item => _ParseGuidReference(item))                
                .ToArray();
        }        

        public static void SetReferenceIds(this IPropertyProvider properties, string key, params Guid[] values)
        {
            var array = values
                .Where(item => item != ProjectDOM.RESETTODEFAULT)
                .Select(item => _ToNodeReference(item))                
                .ToArray();

            if (array.Length == 0) array = null;

            properties.SetArray(key, array);
        }        
    }
    
    sealed class _PropertyLayer : IPropertyProvider
    {
        #region lifecycle

        public _PropertyLayer(IPropertyProvider base_, IPropertyProvider current) { _Base = base_; _Current = current; }

        #endregion

        #region data

        private readonly IPropertyProvider _Base;
        private readonly IPropertyProvider _Current;

        #endregion

        #region API - IPropertyProvider

        public void Clear(string serializationKey)
        {
            _Current.Clear(serializationKey);
        }

        public bool Contains(string serializationKey)
        {
            return _Current.Contains(serializationKey);
        }

        public string GetDefaultValue(string serializationKey, string defval)
        {
            return _Base == null ? defval : _Base.GetValue(serializationKey, defval);
        }        

        public string GetValue(string serializationKey, string defval)
        {
            var underlayingValue = GetDefaultValue(serializationKey, defval);

            return _Current.GetValue(serializationKey, underlayingValue);
        }

        public bool SetValue(string serializationKey, string value)
        {
            return _Current.SetValue(serializationKey, value);
        }

        public string[] GetDefaultArray(string serializationKey, string[] defval)
        {
            return _Base == null ? defval : _Base.GetArray(serializationKey, defval);
        }

        public string[] GetArray(string serializationKey, string[] defval)
        {
            var underlayingCollection = GetDefaultArray(serializationKey, defval);

            return _Current.GetArray(serializationKey, underlayingCollection);
        }        

        public bool SetArray(string serializationKey, string[] array)
        {
            return _Current.SetArray(serializationKey, array);
        }

        // this is an attempt to use an array with the same override rules as single values.
        public string[] GetArray(string serializationKey, Predicate<String> isEmptyFunc, string[] defval)
        {
            var underlayingCollection = GetDefaultArray(serializationKey, defval).ToList();
            var currentCollection =  _Current.GetArray(serializationKey, new String[0]).ToList();

            // remove "empty" trailing elements
            _TrimBack(underlayingCollection, isEmptyFunc);
            _TrimBack(currentCollection, isEmptyFunc);

            var len = Math.Max(underlayingCollection.Count, currentCollection.Count);

            var finalCollection = new String[len];

            for(int i =0; i < finalCollection.Length; ++i)
            {
                if (i <= currentCollection.Count && !isEmptyFunc(currentCollection[i])) { finalCollection[i] = currentCollection[i]; continue; }
                if (i <= underlayingCollection.Count) { finalCollection[i] = underlayingCollection[i]; continue; }
                finalCollection[i] = null;
            }

            return finalCollection.ToArray();
        }

        private static void _TrimBack(IList<string> collection, Predicate<String> isEmptyFunc)
        {
            while (collection.Count > 0)
            {
                var lastIdx = collection.Count - 1;
                if (!isEmptyFunc(collection[lastIdx])) break;
                collection.RemoveAt(lastIdx);
            }
        }

        #endregion
    }
    
    sealed class _ReadOnlyLayer : IPropertyProvider
    {
        public _ReadOnlyLayer(IPropertyProvider props) { _Properties = props; }

        private readonly IPropertyProvider _Properties;

        public void Clear(string serializationKey) { throw new NotSupportedException(); }

        public bool Contains(string serializationKey) { return _Properties.Contains(serializationKey); }

        public string[] GetArray(string serializationKey, string[] defval) { return _Properties.GetArray(serializationKey, defval); }

        public string GetDefaultValue(string serializationKey, string defval) { return _Properties.GetDefaultValue(serializationKey, defval); }

        public string GetValue(string serializationKey, string defval) { return _Properties.GetValue(serializationKey, defval); }

        public bool SetArray(string serializationKey, string[] array) { throw new NotSupportedException(); }

        public bool SetValue(string serializationKey, string value) { throw new NotSupportedException(); }
    }
}
