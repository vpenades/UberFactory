using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static partial class ProjectDOM
    {
        public partial class Configuration : ObjectBase
        {
            public Configuration(string[] cfg)
            {
                if (cfg == null || cfg.Length == 0) throw new ArgumentNullException(nameof(cfg));
                if (!Evaluation.BuildContext.IsValidConfiguration(cfg)) throw new ArgumentException(nameof(cfg));

                Attributes[PROP_NAME] = string.Join(Evaluation.BuildContext.ConfigurationSeparator.ToString(), cfg);
            }

            private const String PROP_NAME = "Name";

            public String ConfigurationFullName => Attributes.GetValueOrDefault(PROP_NAME);

            public String[] ConfigurationPath => ConfigurationFullName.Split(Evaluation.BuildContext.ConfigurationSeparator);            

            public bool IsMatch(string[] cfg)
            {
                if (cfg == null) return false;
                if (!Evaluation.BuildContext.IsValidConfiguration(cfg)) return false;

                return IsMatch(string.Join(Evaluation.BuildContext.ConfigurationSeparator.ToString(), cfg));
            }

            public bool IsMatch(string cfg) { return string.Equals(cfg, ConfigurationFullName, StringComparison.OrdinalIgnoreCase); }

            internal void RemapLocalIds(IReadOnlyDictionary<Guid, Guid> ids)
            {
                throw new NotImplementedException();
            }

        }

        public partial class Node : ObjectBase , IBindableObject
        {
            #region lifecycle

            public static Node Create(Factory.ContentBaseTypeInfo node)
            {
                var filter = node as Factory.ContentFilterTypeInfo;
                if (filter == null) return null;                

                return Create(filter.SerializationKey);
            }

            public static Node Create(string classid)
            {
                if (string.IsNullOrWhiteSpace(classid)) throw new ArgumentNullException(nameof(classid));

                return new Node(classid, Guid.NewGuid());
            }

            private Node(string classid, Guid id)
            {
                ClassIdentifier = classid;
                Identifier = id;                
            }

            #endregion

            #region properties

            private const String _PROP_ID = "Id";
            private const String _PROP_CLASSNAME = "ClassName";
            private const String _PROP_TEMPLATENAME = "TemplateName";

            public Guid Identifier
            {
                get { return Guid.TryParse(Attributes.GetValueOrDefault(_PROP_ID), out Guid v) ? v : Guid.Empty; }
                private set { Attributes[_PROP_ID] = value.ToString(); }
            }

            public string ClassIdentifier
            {
                get { return Properties.GetValue(_PROP_CLASSNAME, null); }
                private set { Properties.SetValue(_PROP_CLASSNAME,value); }
            }

            public string TemplateIdentifier
            {
                get { return Properties.GetValue(_PROP_TEMPLATENAME, null); }
                set { Properties.SetValue(_PROP_TEMPLATENAME, value); }
            }

            public IEnumerable<string> AllConfigurations => GetLogicalChildren<Configuration>().Select(item => item.ConfigurationFullName);

            #endregion

            #region API

            private Configuration _GetConfiguration(params string[] chain)
            {
                return GetLogicalChildren<Configuration>().FirstOrDefault(item => item.IsMatch(chain));
            }

            private Configuration _UseConfiguration(params string[] chain)
            {
                var cfg = _GetConfiguration(chain);

                if (cfg == null)
                {
                    cfg = new Configuration(chain);
                    AddLogicalChild(cfg);
                }

                return cfg;
            }

            public IPropertyProvider GetPropertiesForConfiguration(params string[] configuration)
            {
                if (configuration == null) configuration = new String[] { "Root" };                

                IPropertyProvider provider = null;

                for(int i=0; i < configuration.Length; ++i)
                {
                    var xcfg = configuration.Take(i + 1).ToArray();

                    if (provider == null) provider = _UseConfiguration(xcfg).Properties;
                    else provider = new _PropertyLayer(provider, _UseConfiguration(xcfg).Properties);
                }

                return provider;
            }

            internal void RemapLocalIds(IReadOnlyDictionary<Guid, Guid> ids)
            {
                this.Identifier = ids[this.Identifier];

                foreach(var cfg in GetLogicalChildren<Configuration>())
                {
                    cfg.RemapLocalIds(ids);
                }
            }

            #endregion
        }

        public partial class Pipeline : ObjectBase
        {            
            private const String _PROP_ROOTNODEREF = "RootNodeId";

            #region lifecycle

            public Pipeline() { }

            #endregion

            #region data            

            public IReadOnlyList<Node> Nodes => GetLogicalChildren<Node>().ToArray();

            public Guid RootIdentifier
            {
                get { return Guid.TryParse(Properties.GetValue(_PROP_ROOTNODEREF, null), out Guid v) ? v : Guid.Empty; }
                set { Properties.SetValue(_PROP_ROOTNODEREF, value.ToString()); }
            }

            #endregion

            #region API

            public void ClearNodes() { ClearLogicalChildren<Node>(); RootIdentifier = Guid.Empty; }

            public Node GetNode(Guid nodeId) { return FindBindableObject<Node>(nodeId); }

            public Node GetRootNode() { return GetNode(this.RootIdentifier); }

            public Guid AddNode(Factory.ContentBaseTypeInfo t)
            {
                var f = Node.Create(t); if (f == null) return Guid.Empty;

                AddLogicalChild(f);
                return f.Identifier;
            }

            public Guid AddNode(string className)
            {
                var f = Node.Create(className); if (f == null) return Guid.Empty;

                AddLogicalChild(f);
                return f.Identifier;
            }

            public void RemoveNode(Node f) { RemoveLogicalChild(f); }            

            #endregion
        }

        public partial class Settings : Item
        {
            #region lifecycle

            internal Settings() { }

            #endregion

            #region properties

            public string ClassName
            {
                get
                {
                    var pipeline = GetLogicalChildren<Pipeline>().FirstOrDefault();
                    if (pipeline == null) return null;
                    return pipeline.GetRootNode()?.ClassIdentifier;
                }
            }

            public Pipeline Pipeline
            {
                get
                {
                    var pipeline = GetLogicalChildren<Pipeline>().FirstOrDefault();

                    if (pipeline == null)
                    {
                        pipeline = new Pipeline();
                        AddLogicalChild(pipeline);
                    }

                    return pipeline;
                }
            }

            #endregion
        }

        public partial class Task : Item
        {
            private const String PROP_TITLE = "Title";
            private const String PROP_ENABLED = "Enabled";

            #region lifecycle

            internal Task() { }

            #endregion

            #region properties

            public Boolean Enabled
            {
                get { return Properties.GetValue(PROP_ENABLED, null) != "false"; }
                set { Properties.SetValue(PROP_ENABLED, value ? null : "false"); }
            }

            public String Title
            {
                get { return Properties.GetValue(PROP_TITLE, null); }
                set { Properties.SetValue(PROP_TITLE, value); }
            }            

            public Pipeline Pipeline
            {
                get
                {
                    var pipeline = GetLogicalChildren<Pipeline>().FirstOrDefault();

                    if (pipeline == null)
                    {
                        pipeline = new Pipeline();
                        AddLogicalChild(pipeline);                        
                    }

                    return pipeline;
                }
            }

            #endregion
        }


        public partial class TemplateParameter : ObjectBase
        {
            private const String PROP_NAME = "BindingName";
            private const String PROP_NODEID = "NodeId";
            private const String PROP_PROPID = "NodeProperty";

            #region properties

            public String BindingName
            {
                get { return Properties.GetValue(PROP_NAME, null); }
                set { Properties.SetValue(PROP_NAME, value); }
            }

            public Guid NodeId
            {
                get { return Guid.TryParse(Properties.GetValue(PROP_NODEID, null), out Guid v) ? v : Guid.Empty; }
                set { Properties.SetValue(PROP_NODEID, value.ToString()); }
            }

            public String NodeProperty
            {
                get { return Properties.GetValue(PROP_PROPID, null); }
                set { Properties.SetValue(PROP_PROPID, value); }
            }            

            #endregion
        }

        public partial class Template : Item , IBindableObject
        {
            private const String PROP_ID = "Id";
            private const String PROP_DESCRIPTION = "Description";
            private const String PROP_TITLE = "Title";            

            #region lifecycle

            internal Template()
            {
                Identifier = Guid.NewGuid();                
            }

            #endregion

            #region properties

            public Guid Identifier
            {
                get { return Guid.TryParse(Attributes.GetValueOrDefault(PROP_ID), out Guid v) ? v : Guid.Empty; }
                private set { Attributes[PROP_ID] = value.ToString(); }
            }

            public String Title
            {
                get { return Properties.GetValue(PROP_TITLE, null); }
                set { Properties.SetValue(PROP_TITLE, value); }
            }

            public String Description
            {
                get { return Properties.GetValue(PROP_DESCRIPTION, null); }
                set { Properties.SetValue(PROP_DESCRIPTION, value); }
            }

            public Pipeline Pipeline
            {
                get
                {
                    var pipeline = GetLogicalChildren<Pipeline>().FirstOrDefault();

                    if (pipeline == null)
                    {
                        pipeline = new Pipeline();
                        AddLogicalChild(pipeline);
                    }

                    return pipeline;
                }
            }

            public IEnumerable<TemplateParameter> Parameters => this.GetLogicalChildren<TemplateParameter>();

            #endregion

            #region API

            public void RemoveParameter(TemplateParameter param) { this.RemoveLogicalChild(param); }

            public void AddNewParameter() { this.AddLogicalChild(new TemplateParameter()); }

            public TemplateParameter GetParameterByBindingName(string bname) { return Parameters.FirstOrDefault(item => item.BindingName == bname); }

            #endregion
        }

        public partial class PluginReference : Item
        {
            private const string PROP_ABSPATH = "AbsolutePath";
            private const string PROP_RELPATH = "RelativePath";

            #region lifecycle

            internal PluginReference() { }

            #endregion

            #region properties

            public PathString RelativePath
            {
                get { return new PathString(Properties.GetValue(PROP_RELPATH, null)); }
                set { Properties.SetValue(PROP_RELPATH, value); }
            }

            #endregion
        }
        
        public partial class Project : ObjectBase
        {
            #region lifecycle

            internal Project()
            {
                Attributes["Version"] = _CurrentVersion.ToString();
            }

            #endregion

            #region properties

            public IReadOnlyList<Item> Items => GetLogicalChildren<Item>().ToArray();

            public IReadOnlyList<PathString> References => GetLogicalChildren<PluginReference>().Select(item => item.RelativePath).ToArray();           

            #endregion            

            #region API            

            public bool ContainsReference(PathString rpath)
            {
                return GetLogicalChildren<PluginReference>().Any(item => item.RelativePath == rpath);
            }

            public void InsertReference(PathString rpath)
            {
                var pr = new PluginReference
                {
                    RelativePath = rpath
                };

                AddLogicalChild(pr);
            }

            public void RemoveReference(PathString rpath)
            {
                var child = GetLogicalChildren<PluginReference>().FirstOrDefault(item => item.RelativePath == rpath);
                if (child == null) return;

                RemoveLogicalChild(child);
            }


            public Task AddTask() { var task = new Task(); AddLogicalChild(task); return task; }

            public Template AddTemplate() { var template = new Template(); AddLogicalChild(template); return template; }

            public void RemoveItem(Item item) { RemoveLogicalChild(item); }

            public Template GetTemplate(Guid id) { return this.FindBindableObject<Template>(id); }            

            public void Paste(Guid id)
            {

            }
            
            public Settings UseSettings(string className)
            {
                var s = Items
                    .OfType<Settings>()
                    .FirstOrDefault(item => item.ClassName == className);

                if (s != null) return s;

                s = new Settings(); AddLogicalChild(s);

                var rootId = s.Pipeline.AddNode(className);

                s.Pipeline.RootIdentifier = rootId;

                return s;
            }

            public Settings UseSettings(Type t)
            {
                var skey = t.GetContentTypeInfo()?.SerializationKey;
                if (string.IsNullOrWhiteSpace(skey)) return null;

                return UseSettings(skey);
            }

            #endregion
        }

    }
}
