﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    using Serialization;

    public static partial class ProjectDOM
    {
        public partial class Configuration : ObjectBase
        {
            #region lifecycle

            public Configuration(string[] cfg)
            {
                if (cfg == null || cfg.Length == 0) throw new ArgumentNullException(nameof(cfg));
                if (!Evaluation.BuildContext.IsValidConfiguration(cfg)) throw new ArgumentException(nameof(cfg));

                Attributes[PROP_NAME] = string.Join(Evaluation.BuildContext.ConfigurationSeparator.ToString(), cfg);
            }

            #endregion

            #region data

            private const String PROP_NAME = "Name";

            public String ConfigurationFullName => Attributes.GetValueOrDefault(PROP_NAME);

            public String[] ConfigurationPath => ConfigurationFullName.Split(Evaluation.BuildContext.ConfigurationSeparator);

            #endregion

            #region API

            public bool IsMatch(string[] cfg)
            {
                if (cfg == null) return false;
                if (!Evaluation.BuildContext.IsValidConfiguration(cfg)) return false;

                return IsMatch(string.Join(Evaluation.BuildContext.ConfigurationSeparator.ToString(), cfg));
            }

            public bool IsMatch(string cfg) { return string.Equals(cfg, ConfigurationFullName, StringComparison.OrdinalIgnoreCase); }            

            internal void _RemapLocalIds(IReadOnlyDictionary<Guid, Guid> ids)
            {
                foreach(var k in this.Properties.Keys)
                {
                    var p = this.Properties._GetProperty(k);

                    p._RemapLocalIds(ids);
                }
            }

            #endregion

        }

        [System.Diagnostics.DebuggerDisplay("Node {" + nameof(Node.ClassIdentifier) + "} {" + nameof(Node.Identifier) +"}")]
        public partial class Node : ObjectBase , IBindableObject
        {
            #region lifecycle

            internal static Node Create(Factory.ContentBaseInfo node)
            {
                if (!(node is Factory.ContentFilterInfo filter)) return null;

                return Create(filter.SerializationKey);
            }

            internal static Node Create(string classid)
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

            /// <summary>
            /// Used to remap the ids when we do a deep clone operations of a Pipeline
            /// </summary>
            /// <param name="ids">A dictionary mapping the old IDs to the new IDs</param>
            internal void _RemapLocalIds(IReadOnlyDictionary<Guid, Guid> ids)
            {
                this.Identifier = ids[this.Identifier];

                foreach(var cfg in GetLogicalChildren<Configuration>())
                {
                    cfg._RemapLocalIds(ids);
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

            internal Pipeline() { }            

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

            internal void _RemapIds()
            {
                // create a map of new Guids to use as replacement
                var idMap = new Dictionary<Guid, Guid>();                
                foreach (var n in Nodes) idMap[n.Identifier] = Guid.NewGuid();

                // begin replacement
                this.RootIdentifier = idMap[this.RootIdentifier];

                foreach (var n in Nodes) n._RemapLocalIds(idMap);
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

            public Task CreateDeepCopy(bool remapIds)
            {
                var newTask = new Unknown(this).Activate(_Factory) as Task;                

                newTask.Title += "_Copy";

                if (!remapIds) return newTask;

                newTask.RemapIdentifiers();

                return newTask;
            }            

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

            #region partial serialization

            public void RemapIdentifiers()
            {
                var pipeline = this.GetLogicalChildren<Pipeline>().FirstOrDefault();

                if (pipeline != null) pipeline._RemapIds();
            }

            public System.Xml.Linq.XElement ToXml() { return new Unknown(this).ToXml(); }

            public static Task Parse(System.Xml.Linq.XElement element) { return Unknown.ParseXml(element,_Factory) as Task; }

            #endregion
        }

        public partial class PluginReference : Item
        {
            #region constants

            private const string PROP_ASSEMBLYPATH = "AssemblyPath";
            private const string PROP_PACKAGEID = "PackageId";
            private const string PROP_VERSION = "Version";

            #endregion

            #region lifecycle

            internal PluginReference() { }

            #endregion

            #region properties

            public PathString AssemblyPath
            {
                get { return new PathString(Properties.GetValue(PROP_ASSEMBLYPATH, null)); }
                set { Properties.SetValue(PROP_ASSEMBLYPATH, value); }
            }

            public String Version
            {
                get { return Properties.GetValue(PROP_VERSION, null); }
                set { Properties.SetValue(PROP_VERSION, value); }
            }

            #endregion
        }

        public partial class DocumentInfo : ObjectBase
        {
            #region constants

            private const string PROP_COPYRIGHT = "Copyright";
            private const string PROP_GENERATOR = "Generator";
            private const string PROP_DATE = "Date";

            #endregion

            #region lifecycle

            internal DocumentInfo() { }

            #endregion

            #region properties

            public String Copyright
            {
                get { return Properties.GetValue(PROP_COPYRIGHT, null); }
                set { Properties.SetValue(PROP_COPYRIGHT, value); }
            }

            public String Generator
            {
                get { return Properties.GetValue(PROP_GENERATOR, null); }
                set { Properties.SetValue(PROP_GENERATOR, value); }
            }

            public DateTime Date
            {
                get
                {
                    var val = Properties.GetValue(PROP_DATE, DateTime.Now.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return DateTime.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime date) ? date : DateTime.Now;
                }
                set { Properties.SetValue(PROP_DATE, value.ToString()); }
            }

            #endregion
        }

        public partial class Project : ObjectBase
        {
            #region lifecycle

            internal Project()
            {
                Attributes["Version"] = CurrentVersion.ToString();
            }

            #endregion

            #region properties

            public IReadOnlyList<Item> Items => GetLogicalChildren<Item>().ToArray();

            public IReadOnlyList<PathString> References => GetLogicalChildren<PluginReference>().Select(item => item.AssemblyPath).ToArray();

            public string Copyright { get { return _UseDocumentInfo().Copyright; } set { _UseDocumentInfo().Copyright = value; } }

            public string Generator { get { return _UseDocumentInfo().Generator; } set { _UseDocumentInfo().Generator = value; } }

            public DateTime Date { get { return _UseDocumentInfo().Date; } set { _UseDocumentInfo().Date = value; } }

            #endregion

            #region API

            private DocumentInfo _UseDocumentInfo()
            {
                var info = this.GetLogicalChildren<DocumentInfo>().FirstOrDefault();
                if (info == null) { info = new DocumentInfo(); this.AddLogicalChild(info); }

                return info;
            }

            public bool ContainsReference(PathString rpath)
            {
                return GetLogicalChildren<PluginReference>().Any(item => item.AssemblyPath == rpath);
            }

            public void UseAssemblyReference(PathString rpath, String version)
            {
                var child = GetLogicalChildren<PluginReference>().FirstOrDefault(item => item.AssemblyPath == rpath);

                if (child == null)
                {
                    child = new PluginReference { AssemblyPath = rpath };

                    AddLogicalChild(child);
                }

                child.Version = version;                
            }

            public void RemoveReference(PathString rpath)
            {
                var child = GetLogicalChildren<PluginReference>().FirstOrDefault(item => item.AssemblyPath == rpath);
                if (child == null) return;

                RemoveLogicalChild(child);
            }

            public Task AddTask()
            {
                var task = new Task();

                AddLogicalChild(task);

                return task;
            }

            public Task AddTaskCopy(Task task)
            {
                var newTask = task.CreateDeepCopy(true);

                AddLogicalChild(newTask);

                return newTask;
            }

            public void RemoveItem(Item item) { RemoveLogicalChild(item); }
            
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
