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

        [System.Diagnostics.DebuggerDisplay("Node {" + nameof(Node.ClassIdentifier) + "} {" + nameof(Node.Identifier) +"}")]
        public partial class Node : ObjectBase , IBindableObject
        {
            #region lifecycle

            public static Node Create(Factory.ContentBaseInfo node)
            {
                var filter = node as Factory.ContentFilterInfo;
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

            public bool ReferencesNode(Node node)
            {
                if (node == null) return false;
                if (node == this) return true;

                // node references are simply GUID parseable strings that must match
                // an existing node reference. But it can also happen that an end user
                // chooses to enter a GUID to a text field, that matches that of an
                // existing node (which teoretically should not happen since Node
                // reference GUIDs are internally generated and should never collision
                // with user generated GUIDs.

                // In any case we have to check ALL the properties for ALL configurations.

                foreach (var cfg in AllConfigurations)
                {
                    var splitCfg = cfg.Split(Evaluation.BuildContext.ConfigurationSeparator);

                    var props = _GetConfiguration(splitCfg).Properties;

                    foreach(var key in props.Keys)
                    {
                        var ids = props.GetReferenceIds(key);

                        if (ids.Contains(node.Identifier)) return true;
                    }                    
                }

                return false;
            }

            public int GetHierarchyFingerPrint()
            {
                int h = Identifier.GetHashCode();

                /* TODO: should loop through all referenced nodes
                foreach (var n in this.Nodes)
                {
                    h *= 17;
                    h ^= n.GetHierarchyFingerPrint();
                }*/

                return h;
            }

            #endregion
        }

        [System.Diagnostics.DebuggerDisplay("Pipeline {" + nameof(Pipeline.RootIdentifier) + "}")]
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

            public Guid AddNode(Factory.ContentBaseInfo t)
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

            public void RemoveIsolatedNodes()
            {
                var isolatedNodes = Nodes
                    .Where(item => !_IsBeingReferenced(item))
                    .ToArray();

                foreach (var n in isolatedNodes) RemoveNode(n);
            }

            private bool _IsBeingReferenced(Node node)
            {
                if (node.Identifier == RootIdentifier) return true;

                var otherNodes = Nodes.Where(item => item != node).ToArray();

                return otherNodes.Any(item => item.ReferencesNode(node));
            }

            public int GetHierarchyFingerPrint()
            {
                int h = RootIdentifier.GetHashCode();

                foreach(var n in this.Nodes)
                {
                    h *= 17;
                    h ^= n.GetHierarchyFingerPrint();
                }

                return h;
            }

            #endregion
        }

        [System.Diagnostics.DebuggerDisplay("Settings {" + nameof(Settings.ClassName) + "}")]
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

        [System.Diagnostics.DebuggerDisplay("Task {" + nameof(Task.Title) + "}")]
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

            public void RemoveItem(Item item) { RemoveLogicalChild(item); }            

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
                var skey = t.GetContentInfo()?.SerializationKey;
                if (string.IsNullOrWhiteSpace(skey)) return null;

                return UseSettings(skey);
            }

            #endregion
        }

    }
}
