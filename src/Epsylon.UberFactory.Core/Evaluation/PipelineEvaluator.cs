using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;



namespace Epsylon.UberFactory.Evaluation
{
    
    using INSTANCEFACTORY = Func<String, SDK.ContentObject>;
    using SETTINGSFUNCTION = Func<Type, ProjectDOM.Settings>;

    using SETTINGSINSTANCES = HashSet<SDK.ContentObject>;

    public class PipelineEvaluator
    {
        #region lifecycle        

        public static PipelineEvaluator CreatePipelineInstance(ProjectDOM.Pipeline pipeline, INSTANCEFACTORY filterResolver, SETTINGSFUNCTION settingsResolver)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (filterResolver == null) throw new ArgumentNullException(nameof(filterResolver));            

            return new PipelineEvaluator(pipeline, filterResolver, settingsResolver);
        }        

        private PipelineEvaluator(ProjectDOM.Pipeline pipeline, INSTANCEFACTORY filterResolver, SETTINGSFUNCTION settingsResolver)
        {
            _InstanceFactory = filterResolver;
            _SettingsFactory = settingsResolver;            
            _Pipeline = pipeline;
            _PipelineFingerPrint = pipeline.GetHierarchyFingerPrint();
        }        

        #endregion

        #region data        

        private readonly INSTANCEFACTORY _InstanceFactory;
        private readonly SETTINGSFUNCTION _SettingsFactory;
        
        private readonly ProjectDOM.Pipeline _Pipeline;
        private readonly int _PipelineFingerPrint;
        
        // evaluation data

        private SDK.IBuildContext _BuildSettings;

        private readonly Dictionary<Guid, SDK.ContentObject> _NodeInstances = new Dictionary<Guid, SDK.ContentObject>();        

        private readonly SETTINGSINSTANCES _SettingsInstancesCache = new SETTINGSINSTANCES();

        private readonly List<Guid> _NodeOrder = new List<Guid>(); // ids in order of evaluation

        private _TaskFileIOTracker _FileIOTracker;

        #endregion

        #region properties

        public string InferredTitle
        {
            get
            {
                // tries to generate a title based on the content of the nodes

                var rootInstance = _NodeInstances.GetValueOrDefault(_Pipeline.RootIdentifier);

                return rootInstance is SDK.FileWriter exportInstance ? exportInstance.FileName : "Unknown";
            }
        }        

        #endregion

        #region API - Setup

        public void Setup(SDK.IBuildContext bsettings)
        {
            _BuildSettings = bsettings ?? throw new ArgumentNullException(nameof(bsettings));

            _FileIOTracker = new _TaskFileIOTracker(_BuildSettings);

            _NodeInstances.Clear();
            _NodeOrder.Clear();            

            var nodeIds = new Stack<Guid>();

            _CreateNodeInstancesRecursive(_Pipeline.RootIdentifier, nodeIds);

            _NodeOrder.AddRange(nodeIds.Reverse());
        }

        private void _SetupIsReady()
        {
            if (_BuildSettings == null) throw new InvalidOperationException($"Call {nameof(Setup)} first");

            bool allInstancesReady = !_NodeInstances.Values
                .OfType<_UnknownNode>()
                .Any();

            if (!allInstancesReady) throw new InvalidOperationException("Some filters couldn't be instantiated.");

            if (_PipelineFingerPrint != _Pipeline.GetHierarchyFingerPrint()) throw new InvalidOperationException("DOM hierarchy has changed, call Setup again");
        }

        /// <summary>
        /// Creates a filter instance for a given node ID
        /// </summary>
        /// <remarks>
        /// Creates the instace for the current ID, and resolves the dependencies of the whole tree.
        /// </remarks>
        /// <param name="nodeId">root node id</param>
        private void _CreateNodeInstancesRecursive(Guid nodeId, Stack<Guid> idStack)
        {
            _SetupIsReady();

            if (idStack.Contains(nodeId)) throw new ArgumentException("Circular reference detected: " + nodeId, nameof(nodeId));            

            // Find node DOM
            var nodeDom = _Pipeline.GetNode(nodeId);
            if (nodeDom == null) return;            

            // Create node instance
            var nodeInst = _InstanceFactory(nodeDom.ClassIdentifier);
            SDK.ConfigureNode(nodeInst, _BuildSettings, t=> _GetSettingsInstance(t, null) , _FileIOTracker);

            _NodeInstances[nodeId] = nodeInst ?? throw new NullReferenceException("Couldn't create Node instance for ClassID: " + nodeDom.ClassIdentifier);
            
            // retrieve property values from current cunfiguration
            var properties = nodeDom
                .GetPropertiesForConfiguration(_BuildSettings.Configuration)
                .AsReadOnly();

            // bind property dependencies to instance
            var bindings = nodeInst.CreateBindings(properties).OfType<Bindings.DependencyBinding>();

            // recursively create dependencies
            foreach(var binding in bindings)
            {
                if (binding is Bindings.SingleDependencyBinding)
                {
                    var id = ((Bindings.SingleDependencyBinding)binding).GetDependency();

                    _CreateNodeInstancesRecursive(id, idStack);
                }

                if (binding is Bindings.MultiDependencyBinding)
                {
                    var ids = ((Bindings.MultiDependencyBinding)binding).GetDependencies();

                    foreach(var id in ids) _CreateNodeInstancesRecursive(id, idStack);
                }
            }

            idStack.Push(nodeId);
        }        

        #endregion

        #region API - Evaluation

        public SDK.ContentObject GetNodeInstance(Guid id)
        {
            _SetupIsReady();

            return _NodeInstances.GetValueOrDefault(id);
        }                

        public IEnumerable<Bindings.MemberBinding> CreateBindings(Guid nodeId)
        {
            _SetupIsReady();

            // get source and destination objects            
            var nodeProps = _Pipeline.GetNode(nodeId)?.GetPropertiesForConfiguration(_BuildSettings.Configuration);
            var nodeInst = _NodeInstances.GetValueOrDefault(nodeId);            

            // evaluate only the values
            nodeInst.EvaluateBindings(nodeProps, null); // we don't pass the dependency evaluator callback because we only want to assign the initial values, we don't want a full chain evaluation

            return nodeInst.CreateBindings(nodeProps);
        }       
        
        
        
        public Object EvaluateRoot(SDK.IMonitorContext monitor)
        {
            _SetupIsReady();

            _FileIOTracker?.Clear();

            return EvaluateNode(monitor, _Pipeline.RootIdentifier);
        }

        public Object EvaluateNode(SDK.IMonitorContext monitor, Guid nodeId)
        {
            _SetupIsReady();

            _SettingsInstancesCache.Clear();

            return _EvaluateNode(monitor, nodeId, false);
        }

        public IPreviewResult PreviewNode(SDK.IMonitorContext monitor, Guid nodeId)
        {
            _SetupIsReady();

            _FileIOTracker?.Clear();

            _SettingsInstancesCache.Clear();

            var previewObject =  _EvaluateNode(monitor, nodeId, true);

            if (previewObject is _DictionaryExportContext expDict)
            {
                return expDict;
            }

            if (previewObject is IConvertible convertible)
            {
                var text = convertible.ToString();
                var data = Encoding.UTF8.GetBytes(text);

                var dict = _DictionaryExportContext.Create("preview.txt", null);

                dict.WriteAllBytes(data);

                return dict;
            }

            return null;
        }

        private Object _EvaluateNode(SDK.IMonitorContext monitor, Guid nodeId, bool previewMode)
        {
            if (monitor != null && monitor.IsCancelRequested) throw new OperationCanceledException();

            _SetupIsReady();

            // Get the current node being evaluated
            var nodeInst = _NodeInstances.GetValueOrDefault(nodeId);
            if (nodeInst == null) return null;
            if (nodeInst is _UnknownNode) return null;

            _FileIOTracker.RegisterAssemblyFile(nodeInst.GetType().Assembly.Location);            

            // Next, we retrieve the property values for this node from the DOM
            var nodeProps = _Pipeline
                .GetNode(nodeId)?
                .GetPropertiesForConfiguration(_BuildSettings.Configuration)
                .AsReadOnly();
            if (nodeProps == null) return null;

            // Assign values and node dependencies. Dependecies are evaluated to its values.
            nodeInst.EvaluateBindings(nodeProps,xid => _EvaluateNode(monitor, xid, previewMode));            

            if (monitor != null && monitor.IsCancelRequested) throw new OperationCanceledException();

            // evaluate the current node            

            try
            {
                var localMonitor = monitor?.GetProgressPart(_NodeOrder.IndexOf(nodeId), _NodeOrder.Count);                

                if (nodeInst is SDK.ContentFilter filterInst)
                {
                    if (previewMode) return SDK.PreviewNode(filterInst, localMonitor);
                    else
                    {
                        if (System.Diagnostics.Debugger.IsAttached) return SDK.DebugNode(filterInst, localMonitor);
                        else return SDK.EvaluateNode(filterInst, localMonitor);
                    }
                }
                else if (nodeInst is SDK.ContentObject) return nodeInst;
            }
            catch(Exception ex)
            {
                throw new PluginException(ex);
            }

            throw new NotImplementedException();
        }

        private SDK.ContentObject _GetSettingsInstance(Type t, SDK.IMonitorContext monitor)
        {
            _SetupIsReady();

            var si = _SettingsInstancesCache.FirstOrDefault(item => item.GetType() == t);

            if (si != null) return si;
            if (_SettingsFactory == null) return null;

            var sdom = _SettingsFactory.Invoke(t);
            var spip = CreatePipelineInstance(sdom.Pipeline, _InstanceFactory, _SettingsFactory);
            spip.Setup(_BuildSettings);

            var r = spip.EvaluateRoot(monitor);

            si = r as SDK.ContentObject;

            _SettingsInstancesCache.Add(si);

            return si;
        }

        #endregion        

    }


    [SDK.ContentNode("PLUGIN ERROR")]
    class _UnknownNode : SDK.ContentFilter
    {
        protected override object EvaluateObject()
        {
            return null;
        }
    }


    class _TaskFileIOTracker : SDK.ITaskFileIOTracker
    {
        public _TaskFileIOTracker(SDK.IBuildContext bc)
        {

        }


        private readonly HashSet<String> _AssemblyFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);// assembly files used by this pipeline
        private readonly HashSet<String> _InputFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);   // files read from Source directory
        private readonly HashSet<String> _OutputFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);  // files written to temporary directory

        public IEnumerable<String> InputFiles
        {
            get
            {
                return _InputFiles;
            }
        }

        public IEnumerable<String> OutputFiles
        {
            get
            {
                return _OutputFiles;
            }
        }

        public void Clear()
        {
            _AssemblyFiles.Clear();
            _InputFiles.Clear();
            _OutputFiles.Clear();
        }

        public void RegisterAssemblyFile(string filePath) { _AssemblyFiles.Add(filePath); }

        void SDK.ITaskFileIOTracker.RegisterInputFile(string filePath, string parentFilePath)
        {
            _InputFiles.Add(filePath);
        }

        void SDK.ITaskFileIOTracker.RegisterOutputFile(string filePath, string parentFilePath)
        {
            _OutputFiles.Add(filePath);
        }
    }
}
