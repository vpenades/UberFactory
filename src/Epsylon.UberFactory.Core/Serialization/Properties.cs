using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Epsylon.UberFactory.Serialization
{
    [System.Diagnostics.DebuggerDisplay("{Key} = {System.String.Join(\" \",GetValues())}")]
    public sealed class Property
    {
        #region lifecycle

        internal Property() { }

        internal Property(string key) { _Key = key; }

        internal Property(Property other)
        {
            this._Key = other._Key;
            this._Value = other._Value;
            if (other._Array != null) this._Array = (string[])other._Array.Clone();
        }

        #endregion

        #region data

        private string _Key;
        private string _Value;
        private string[] _Array;

        internal XElement _ToXml()
        {
            if (_Array != null) return new XElement(_Constants.Namespace.GetName(_Key), _Array.Select(item => new XElement(_Constants.PropertyArrayItem, item)));
            if (_Value != null) return new XElement(_Constants.Namespace.GetName(_Key), _Value);
            return new XElement(_Key);
        }

        internal static Property _ParseXml(XElement xml)
        {
            var p = new Property(xml.Name.LocalName);

            if (xml.HasElements)
            {
                p._Array = xml.Elements(_Constants.PropertyArrayItem).Select(item => item.Value).ToArray();
                return p;
            }

            p._Value = xml.Value;

            return p;
        }

        #endregion

        #region properties

        public String Key { get => _Key; set => _Key = value; }

        public int Count
        {
            get
            {
                if (_Array != null) return _Array.Length;
                if (_Value != null) return 1;
                return 0;
            }
        }

        #endregion

        #region API

        public bool SetValues(params String[] values)
        {
            if (values == null)
            {
                if (_Value == null && _Array == null) return false;
                _Value = null;
                _Array = null;
                return true;
            }

            if (values.Length == 1)
            {
                if (_Value == values[0]) return false;
                _Array = null;
                _Value = values[0];
                return true;
            }

            if (_Array != null && Enumerable.SequenceEqual(_Array, values)) return false;

            _Value = null;
            _Array = (string[])values.Clone();
            return true;
        }

        public String GetValue()
        {
            if (_Value != null) return _Value;
            return _Array != null && _Array.Length > 0 ? _Array[0] : null;
        }

        public String[] GetValues()
        {
            return _Value == null ? _Array?.ToArray() : new String[] { _Value };
        }

        internal void _RemapLocalIds(IReadOnlyDictionary<Guid, Guid> ids)
        {
            if (_Value != null)
            {
                if (Guid.TryParse(_Value, out Guid oldId))
                {
                    if (ids.TryGetValue(oldId, out Guid newId)) _Value = newId.ToString();
                }
            }

            if (_Array != null)
            {
                for (int i = 0; i < _Array.Length; ++i)
                {
                    if (_Array[i] != null && Guid.TryParse(_Array[i], out Guid oldId))
                    {
                        if (ids.TryGetValue(oldId, out Guid newId)) _Array[i] = newId.ToString();
                    }
                }
            }
        }

        #endregion
    }

    public sealed class PropertyGroup : IPropertyProvider
    {
        #region data

        private readonly List<Property> _Properties = new List<Property>();

        internal void _ToXml(XElement target)
        {
            target.Add(_Properties.Select(item => item._ToXml()));
        }

        internal void _ParseXml(XElement element)
        {
            _Properties.AddRange(element.Elements().Select(item => Property._ParseXml(item)));
        }

        #endregion

        #region API

        public IEnumerable<string> Keys => _Properties.Select(item => item.Key);

        public void CopyTo(PropertyGroup other) { other._Properties.AddRange(this._Properties.Select(item => new Property(item))); }

        internal Property _GetProperty(string key)
        {
            return _Properties.FirstOrDefault(item => item.Key == key);
        }

        internal Property _UseProperty(string key)
        {
            var p = _GetProperty(key);

            if (p == null)
            {
                p = new Property(key);
                _Properties.Add(p);
            }

            return p;
        }

        public bool Contains(string key) { return _GetProperty(key) != null; }

        public void Clear(string key)
        {
            var idx = _Properties.IndexOf(item => item.Key == key);
            if (idx >= 0) _Properties.RemoveAt(idx);
        }

        private bool _DelProperty(string key)
        {
            int idx = _Properties.IndexOf(item => item.Key == key);
            if (idx < 0) return false;
            _Properties.RemoveAt(idx);
            return true;
        }

        public string GetValue(string key, string defval)
        {
            var p = _GetProperty(key);
            return (p == null) ? defval : p.GetValue();
        }

        public bool SetValue(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            return value == null ? _DelProperty(key) : _UseProperty(key).SetValues(value);
        }

        public string[] GetArray(string key, params string[] defval)
        {
            var p = _GetProperty(key);
            return (p == null) ? defval : p.GetValues();
        }

        public bool SetArray(string key, params string[] array)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            return array == null ? _DelProperty(key) : _UseProperty(key).SetValues(array);
        }

        public string GetDefaultValue(string key, string defval) { return defval; }

        #endregion
    }
}
