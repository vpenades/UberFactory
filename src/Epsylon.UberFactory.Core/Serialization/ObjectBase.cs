using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Epsylon.UberFactory.Serialization
{
    // http://stackoverflow.com/questions/4858798/datacontract-xml-serialization-and-xml-attributes

    class _Constants
    {
        public static readonly XNamespace Namespace = "http://www.uberfactory.com";
        public static readonly XName PropertyArrayItem = Namespace.GetName("Item");        
    }

    public delegate ObjectBase ObjectFactoryDelegate(Unknown obj);

    public delegate Unknown UnknownFactoryDelegate(ObjectBase obj);

    public interface IBindableObject { Guid Identifier { get; } }

    

    public abstract class ObjectBase
    {
        #region lifecycle

        protected ObjectBase() { }

        internal ObjectBase(Unknown other, ObjectFactoryDelegate factory)
        {
            other._Properties.CopyTo(this._Properties);

            foreach (var kvp in other._Attributes) { this._Attributes[kvp.Key] = kvp.Value; }

            foreach (var child in other._LogicalChildren)
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

        public IDictionary<string, string> Attributes => _Attributes;

        public PropertyGroup Properties => _Properties;

        protected IEnumerable<T> GetLogicalChildren<T>() where T : ObjectBase { return _LogicalChildren.OfType<T>(); }

        protected void AddLogicalChild(ObjectBase item) { _LogicalChildren.Add(item); }

        protected void RemoveLogicalChild(ObjectBase item) { _LogicalChildren.Remove(item); }

        protected void ClearLogicalChildren<T>() where T : ObjectBase { _LogicalChildren.RemoveAll(item => item is T); }

        public T FindBindableObject<T>(Guid id) where T : ObjectBase
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
            ClassName = other is Unknown unkOther ? unkOther.ClassName : other.GetType().Name;
        }

        #endregion

        #region properties

        public string ClassName { get; set; }

        public IEnumerable<Unknown> Children => GetLogicalChildren<Unknown>();

        #endregion

        #region API

        public XElement ToXml()
        {
            var root = new XElement(_Constants.Namespace.GetName(ClassName));

            root.Add(Attributes.Select(kvp => new XAttribute(kvp.Key, kvp.Value)).ToArray());

            if (true)
            {
                var props = new XElement(_Constants.Namespace.GetName("Properties"));
                Properties._ToXml(props);
                root.Add(props);
            }

            root.Add(Children.Select(item => item.ToXml()));
            return root;
        }

        public static ObjectBase ParseXml(XElement root, ObjectFactoryDelegate factory)
        {
            var target = new Unknown(root.Name.LocalName);

            foreach (var xattr in root.Attributes())
            {
                target.Attributes[xattr.Name.LocalName] = xattr.Value;
            }

            var props = root.Element(_Constants.Namespace.GetName("Properties"));

            if (props != null) target.Properties._ParseXml(props);

            foreach (var childxml in root.Elements().Where(item => item.Name.LocalName != "Properties"))
            {
                var child = ParseXml(childxml, factory);
                target.AddLogicalChild(child);
            }

            return factory(target);
        }

        public ObjectBase Activate(ObjectFactoryDelegate factory) { return factory(this); }

        #endregion
    }
}
