using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;


namespace Epsylon.UberFactory
{
    // TODO: add Author data, modification date, etc

    public static partial class ProjectDOM
    {
        // http://stackoverflow.com/questions/4858798/datacontract-xml-serialization-and-xml-attributes

        private static readonly XNamespace _Namespace = "http://www.uberfactory.com";        
        private static readonly XName _PropertyArrayItem = _Namespace.GetName("Item");
        private static readonly Version _CurrentVersion = new Version(1, 0);

        public static Version CurrentVersion => _CurrentVersion;

        public interface IBindableObject { Guid Identifier { get; } }

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
                if (_Array != null) return new XElement(_Namespace.GetName(_Key), _Array.Select(item => new XElement(_PropertyArrayItem, item)));
                if (_Value != null) return new XElement(_Namespace.GetName(_Key), _Value);
                return new XElement(_Key);
            }

            internal static Property _ParseXml(XElement xml)
            {
                var p = new Property(xml.Name.LocalName);

                if (xml.HasElements)
                {
                    p._Array = xml.Elements(_PropertyArrayItem).Select(item => item.Value).ToArray();
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
                    for(int i=0; i < _Array.Length; ++i)
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

        public abstract class ObjectBase
        {
            #region lifecycle

            protected ObjectBase() { }

            internal ObjectBase(Unknown other, ObjectFactoryDelegate factory)
            {
                other._Properties.CopyTo(this._Properties);

                foreach (var kvp in other._Attributes) { this._Attributes[kvp.Key] = kvp.Value; }

                foreach(var child in other._LogicalChildren)
                {
                    if (factory != null && child is Unknown unkChild)
                    {
                        _LogicalChildren.Add(factory(unkChild));
                    }
                    else
                    {
                        _LogicalChildren.Add(child);
                    }                    
                }                
            }

            internal ObjectBase(ObjectBase other, UnknownFactoryDelegate toUnknown)
            {
                other._Properties.CopyTo(this._Properties);

                foreach (var kvp in other._Attributes) { this._Attributes[kvp.Key] = kvp.Value; }

                _LogicalChildren.AddRange(other._LogicalChildren.Select(item => toUnknown(item)));
            }

            #endregion

            #region data

            private readonly Dictionary<String, String> _Attributes = new Dictionary<string, string>();

            private readonly PropertyGroup _Properties = new PropertyGroup();

            private readonly List<ObjectBase> _LogicalChildren = new List<ObjectBase>();

            #endregion

            #region API

            public IDictionary<string,string> Attributes    => _Attributes;

            public PropertyGroup Properties                 => _Properties;

            protected IEnumerable<T> GetLogicalChildren<T>() where T : ObjectBase { return _LogicalChildren.OfType<T>(); }

            protected void AddLogicalChild(ObjectBase item) { _LogicalChildren.Add(item); }

            protected void RemoveLogicalChild(ObjectBase item) { _LogicalChildren.Remove(item); }

            protected void ClearLogicalChildren<T>() where T : ObjectBase { _LogicalChildren.RemoveAll(item => item is T); }

            public T FindBindableObject<T>(Guid id) where T:ObjectBase
            {
                if (this is IBindableObject bo && bo.Identifier == id) return this as T;

                return _LogicalChildren
                    .OfType<T>()
                    .Select(item => item.FindBindableObject<T>(id))
                    .ExceptNulls()
                    .FirstOrDefault();
            }            

            #endregion
        }

        public sealed class Unknown : ObjectBase
        {
            #region lifecycle

            public Unknown(string name) { ClassName = name; }

            public Unknown(ObjectBase other) : base(other, o => new Unknown(o))
            {
                ClassName = other is Unknown ? ((Unknown)other).ClassName : other.GetType().Name;
            }

            #endregion

            #region properties

            public string ClassName { get; set; }

            public IEnumerable<Unknown> Children => GetLogicalChildren<Unknown>();

            #endregion

            #region API

            public XElement ToXml()
            {
                var root = new XElement(_Namespace.GetName(ClassName));

                root.Add(Attributes.Select(kvp => new XAttribute(kvp.Key, kvp.Value)).ToArray());                

                if (true)
                {
                    var props = new XElement(_Namespace.GetName("Properties"));
                    Properties._ToXml(props);
                    root.Add(props);
                }

                root.Add(Children.Select(item => item.ToXml()));
                return root;
            }

            public static ObjectBase ParseXml(XElement root, Func<Unknown, ObjectBase> factory)
            {
                var target = new Unknown(root.Name.LocalName);

                foreach(var xattr in root.Attributes())
                {
                    target.Attributes[xattr.Name.LocalName] = xattr.Value;
                }

                var props = root.Element(_Namespace.GetName("Properties"));

                if (props != null) target.Properties._ParseXml(props);

                foreach (var childxml in root.Elements().Where(item => item.Name.LocalName != "Properties"))
                {
                    var child = ParseXml(childxml, factory);
                    target.AddLogicalChild(child);
                }

                return factory(target);
            }

            public ObjectBase Activate() { return _Factory(this); }

            #endregion
        }

        public partial class Configuration : ObjectBase
        {
            internal Configuration(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }
        }        

        public partial class Node : ObjectBase
        {
            internal Node(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }
        }

        public partial class Pipeline : ObjectBase
        {
            internal Pipeline(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }            
        }

        public abstract partial class Item : ObjectBase
        {
            internal Item(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }

            protected Item() {}
        }        

        public partial class Settings : Item
        {
            internal Settings(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }
        }

        public partial class Task : Item
        {
            internal Task(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }

            internal Task() {}
        }        

        public partial class PluginReference : Item
        {
            internal PluginReference(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }
        }

        public partial class DocumentInfo : ObjectBase
        {
            internal DocumentInfo(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }
        }

        public partial class Project : ObjectBase
        {
            internal Project(Unknown s, ObjectFactoryDelegate factory) : base(s, factory)
            {
                Attributes["Version"] = _CurrentVersion.ToString();
            }

            #region serialization                

            public String GetBody(bool useCurrentTime = true)
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    this.Generator = $"{assembly.GetName().Name} {assembly.GetName().Version.ToString()}";
                }

                this.Date = useCurrentTime ? DateTime.Now : DateTime.Now.Date;

                var root = new Unknown(this).ToXml();
                return root.ToString(SaveOptions.OmitDuplicateNamespaces);
            }

            public void SaveTo(string filePath)
            {
                var body = GetBody();
                System.IO.File.WriteAllText(filePath, body);
            }            

            #endregion
        }

    }
}