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

    [System.Diagnostics.DebuggerDisplay("Pipeline {InferredTitle}")]
    public class PipelineInstance
    {
        #region lifecycle        

        public static PipelineInstance CreatePipelineInstance(ProjectDOM.Pipeline pipeline, INSTANCEFACTORY filterResolver, SETTINGSFUNCTION settingsResolver)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (filterResolver == null) throw new ArgumentNullException(nameof(filterResolver));

            return new PipelineInstance(pipeline, filterResolver, settingsResolver);
        }

        private PipelineInstance(ProjectDOM.Pipeline pipeline, INSTANCEFACTORY filterResolver, SETTINGSFUNCTION settingsResolver)
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

        private BuildContext _BuildSettings;

        private readonly Dictionary<Guid, SDK.ContentObject> _NodeInstances = new Dictionary<Guid, SDK.ContentObject>();        

        private readonly List<Guid> _NodeOrder = new List<Guid>(); // ids in order of evaluation        

        #endregion

        #region properties

        public string InferredTitle => _InferTitleFromRootNode();        

        #endregion

        #region API - Setup

        public void Setup(BuildContext bsettings)
        {
            _BuildSettings = bsettings ?? throw new ArgumentNullException(nameof(bsettings));

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

            _NodeInstances[nodeId] = nodeInst ?? throw new NullReferenceException("Couldn't create Node instance for ClassID: " + nodeDom.ClassIdentifier);

            // retrieve property values from current cunfiguration
            var properties = nodeDom
                .GetPropertiesForConfiguration(_BuildSettings.Configuration)
                .AsReadOnly();

            // bind property dependencies to instance
            var bindings = nodeInst.CreateBindings(properties).OfType<Bindings.DependencyBinding>();

            // recursively create dependencies
            foreach (var binding in bindings)
            {
                if (binding is Bindings.SingleDependencyBinding)
                {
                    var id = ((Bindings.SingleDependencyBinding)binding).GetDependency();

                    _CreateNodeInstancesRecursive(id, idStack);
                }

                if (binding is Bindings.MultiDependencyBinding)
                {
                    var ids = ((Bindings.MultiDependencyBinding)binding).GetDependencies();

                    foreach (var id in ids) _CreateNodeInstancesRecursive(id, idStack);
                }
            }

            idStack.Push(nodeId);
        }

        #endregion

        #region API - Evaluation

        private string _InferTitleFromRootNode()
        {
            // tries to generate a title based on the content of the nodes

            var rootInstance = _NodeInstances.GetValueOrDefault(_Pipeline.RootIdentifier);

            if (rootInstance is SDK.FileWriter writer)
            {
                return writer.FileName;
            }

            if (rootInstance is SDK.ContentFilter filter)
            {
                return filter.GetContentInfo().DisplayName;                
            }

            if (rootInstance is SDK.ContentObject settings)
            {
                return settings.GetContentInfo().DisplayName;                
            }

            return "Unknown";
        }

        /// <summary>
        /// Gets the instance object of a given ID
        /// </summary>
        /// <param name="nodeId">the ID of the node</param>
        /// <returns>A Content Instance</returns>
        public SDK.ContentObject GetNodeInstance(Guid nodeId)
        {
            return _NodeInstances.GetValueOrDefault(nodeId);
        }

        /// <summary>
        /// Gets the stored property values of a given ID
        /// </summary>
        /// <param name="nodeId">the ID of the node</param>
        /// <returns>A property values container</returns>
        private IPropertyProvider _GetNodeProperties(Guid nodeId)
        {
            return _Pipeline
                 .GetNode(nodeId)?
                 .GetPropertiesForConfiguration(_BuildSettings.Configuration);
        }

        public IEnumerable<Bindings.MemberBinding> CreateValueBindings(Guid nodeId)
        {
            _SetupIsReady();
            
            var nodeInst = GetNodeInstance(nodeId);
            var nodeProps = _GetNodeProperties(nodeId);

            if (nodeInst == null || nodeProps == null) return Enumerable.Empty<Bindings.MemberBinding>();            

            // evaluate only the values
            nodeInst.EvaluateBindings(nodeProps, null); // we don't pass the dependency evaluator callback because we only want to assign the value properties, we don't want a full recursi evaluation

            return nodeInst.CreateBindings(nodeProps);
        }

        private SDK.ContentObject _CreateSettingsInstance(Type t)
        {
            if (_SettingsFactory == null) return null;

            var sdom = _SettingsFactory.Invoke(t);
            var spip = CreatePipelineInstance(sdom.Pipeline, _InstanceFactory, _SettingsFactory);
            spip.Setup(_BuildSettings);

            using (var evaluator = spip.CreateEvaluator())
            {
                return evaluator.EvaluateRoot().Result as SDK.ContentObject;
            }                
        }

        public PipelineEvaluator CreateEvaluator(SDK.IMonitorContext monitor=null)
        {
            _SetupIsReady();            

            return PipelineEvaluator.Create
                (                
                _BuildSettings,
                _Pipeline.RootIdentifier,
                _NodeOrder.ToArray(),
                GetNodeInstance,
                _CreateSettingsInstance,
                _GetNodeProperties,
                monitor
                );
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
}
