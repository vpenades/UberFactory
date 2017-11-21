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

        private SDK.IBuildContext _BuildSettings;

        private readonly Dictionary<Guid, SDK.ContentObject> _NodeInstances = new Dictionary<Guid, SDK.ContentObject>();        

        private readonly List<Guid> _NodeOrder = new List<Guid>(); // ids in order of evaluation        

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

            SDK.ConfigureNode(nodeInst, _BuildSettings);

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

        public SDK.ContentObject GetNodeInstance(Guid nodeId)
        {
            return _NodeInstances.GetValueOrDefault(nodeId);
        }

        private IPropertyProvider _GetNodeProperties(Guid nodeId)
        {
            return _Pipeline
                 .GetNode(nodeId)?
                 .GetPropertiesForConfiguration(_BuildSettings.Configuration)
                 .AsReadOnly();
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

        private SDK.ContentObject _CreateSettingsInstance(Type t)
        {
            if (_SettingsFactory == null) return null;

            var sdom = _SettingsFactory.Invoke(t);
            var spip = CreatePipelineInstance(sdom.Pipeline, _InstanceFactory, _SettingsFactory);
            spip.Setup(_BuildSettings);

            return spip.GetEvaluator(MonitorContext.CreateNull()).EvaluateRoot() as SDK.ContentObject;
        }

        public PipelineEvaluator GetEvaluator(SDK.IMonitorContext monitor)
        {
            _SetupIsReady();

            return PipelineEvaluator.Create
                (
                monitor,
                _BuildSettings,
                _Pipeline.RootIdentifier,
                _NodeOrder.ToArray(),
                GetNodeInstance,
                _CreateSettingsInstance,
                _GetNodeProperties
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
