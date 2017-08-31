﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;



namespace Epsylon.UberFactory.Evaluation
{
    
    using FILTERFACTORY = Func<String, SDK.ContentObject>;
    using SETTINGSFACTORY = Func<Type, ProjectDOM.Settings>;
    using TEMPLATEFACTORY = Func<Guid, ProjectDOM.Template>;
    

    public class PipelineEvaluator : SDK.IPipelineInstance
    {
        #region lifecycle        

        public static PipelineEvaluator CreatePipelineInstance(ProjectDOM.Pipeline pipeline, FILTERFACTORY filterResolver, SETTINGSFACTORY settingsResolver, TEMPLATEFACTORY templateResolver)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (filterResolver == null) throw new ArgumentNullException(nameof(filterResolver));
            if (templateResolver == null) throw new ArgumentNullException(nameof(templateResolver));            

            return new PipelineEvaluator(pipeline, filterResolver, settingsResolver, templateResolver);
        }

        public static PipelineEvaluator CreatePipelineInstance(ProjectDOM.Template template, FILTERFACTORY filterResolver, SETTINGSFACTORY settingsResolver, TEMPLATEFACTORY templateResolver)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (template.Pipeline == null) throw new ArgumentNullException(nameof(template));
            if (filterResolver == null) throw new ArgumentNullException(nameof(filterResolver));
            if (templateResolver == null) throw new ArgumentNullException(nameof(templateResolver));

            return new PipelineEvaluator(template, filterResolver, settingsResolver, templateResolver);
        }

        private PipelineEvaluator(ProjectDOM.Pipeline pipeline, FILTERFACTORY filterResolver, SETTINGSFACTORY settingsResolver, TEMPLATEFACTORY templateResolver)
        {
            _FilterFactory = filterResolver;
            _SettingsFactory = settingsResolver;
            _TemplateFactory = templateResolver;            

            _Template = null;
            _Pipeline = pipeline;            
        }

        private PipelineEvaluator(ProjectDOM.Template template, FILTERFACTORY filterResolver, SETTINGSFACTORY settingsResolver, TEMPLATEFACTORY templateResolver)
        {
            _FilterFactory = filterResolver;
            _SettingsFactory = settingsResolver;
            _TemplateFactory = templateResolver;

            _Template = template;
            _Pipeline = template.Pipeline;            
        }

        #endregion

        #region data        

        private readonly FILTERFACTORY _FilterFactory;
        private readonly SETTINGSFACTORY _SettingsFactory;
        private readonly TEMPLATEFACTORY _TemplateFactory;

        private readonly ProjectDOM.Template _Template; // Non Null if this evaluator is a template
        private readonly ProjectDOM.Pipeline _Pipeline; // here is the serialized data from where we initialize the instances        
        
        // evaluation data

        private BuildContext _BuildSettings;        
        private readonly Dictionary<Guid, SDK.ContentObject> _NodeInstances = new Dictionary<Guid, SDK.ContentObject>();
        private readonly Dictionary<Guid, PipelineEvaluator> _PipelineInstances = new Dictionary<Guid, PipelineEvaluator>();

        private readonly HashSet<SDK.ContentObject> _SettingsInstancesCache = new HashSet<SDK.ContentObject>();

        private readonly List<Guid> _NodeOrder = new List<Guid>(); // ids in order of evaluation

        #endregion

        #region properties

        public string InferredTitle
        {
            get
            {
                // tries to generate a title based on the content of the nodes

                var rootInstance = _NodeInstances.GetValueOrDefault(_Pipeline.RootIdentifier);

                var exportInstance = rootInstance as SDK.FileWriter;
                if (exportInstance == null) return "Unknown";

                return exportInstance.FileName;
            }
        }        

        #endregion

        #region API - Setup

        public void Setup(BuildContext bsettings)
        {
            _BuildSettings = bsettings ?? throw new ArgumentNullException(nameof(bsettings));            

            _NodeInstances.Clear();
            _NodeOrder.Clear();

            _PipelineInstances.Clear();

            var nodeIds = new Stack<Guid>();

            _CreateNodeInstancesRecursive(_Pipeline.RootIdentifier, nodeIds);

            _NodeOrder.AddRange(nodeIds.Reverse());
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
            if (idStack.Contains(nodeId)) throw new ArgumentException("Circular reference detected: " + nodeId, nameof(nodeId));            

            // Find node DOM
            var nodeDom = _Pipeline.GetNode(nodeId);
            if (nodeDom == null) return;            

            // Create node instance
            var nodeInst = _FilterFactory(nodeDom.ClassIdentifier);
            SDK.ConfigureNode(nodeInst, _BuildSettings, t=> GetSettingsInstance(t,null) );

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
                if (binding is Bindings.PipelineDependencyBinding)
                {
                    var templateId = ((Bindings.PipelineDependencyBinding)binding).GetDependency();                    

                    var templateDom = _TemplateFactory(templateId);

                    if (templateDom == null) _PipelineInstances[templateId] = null;
                    else
                    {
                        var templateInst = CreatePipelineInstance(templateDom, _FilterFactory,_SettingsFactory, _TemplateFactory);
                        templateInst.Setup(_BuildSettings);

                        _PipelineInstances[templateId] = templateInst;
                    }
                }

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

        public SDK.ContentObject GetNodeInstance(Guid id) { return _NodeInstances.GetValueOrDefault(id); }                

        public IEnumerable<Bindings.MemberBinding> CreateBindings(Guid nodeId)
        {
            // get source and destination objects            
            var nodeProps = _Pipeline.GetNode(nodeId)?.GetPropertiesForConfiguration(_BuildSettings.Configuration);
            var nodeInst = _NodeInstances.GetValueOrDefault(nodeId);            

            // evaluate only the values
            nodeInst.EvaluateBindings(nodeProps, null); // we don't pass the dependency evaluator callback because we only want to assign the initial values, we don't want a full chain evaluation

            return nodeInst.CreateBindings(nodeProps);
        }

        public SDK.ContentObject GetSettingsInstance(Type t, SDK.IMonitorContext monitor)
        {
            var si = _SettingsInstancesCache.FirstOrDefault(item => item.GetType() == t);

            if (si != null) return si;
            if (_SettingsFactory == null) return null;

            var sdom = _SettingsFactory.Invoke(t);
            var spip = CreatePipelineInstance(sdom.Pipeline, _FilterFactory,_SettingsFactory, _TemplateFactory);
            spip.Setup(_BuildSettings);

            var r = spip.Evaluate(monitor);

            si = r as SDK.ContentObject;

            _SettingsInstancesCache.Add(si);

            return si;
        }
        
        
        /// <summary>
        /// called from a template evaluator
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Object Evaluate(SDK.IMonitorContext monitor, params Object[] parameters)
        {
            if (_NodeInstances.Values.OfType<_UnknownNode>().Any()) throw new InvalidOperationException("Some filters couldn't be instantiated.");

            _SettingsInstancesCache.Clear();

            var rootInstance = _NodeInstances.GetValueOrDefault(_Pipeline.RootIdentifier);            

            return _EvaluateNode(monitor, _Pipeline.RootIdentifier, false, parameters);
        }

        public Object EvaluateNode(SDK.IMonitorContext monitor, Guid nodeOrTemplateId)
        {
            _SettingsInstancesCache.Clear();

            return _EvaluateNode(monitor, nodeOrTemplateId, false);
        }

        public Object PreviewNode(SDK.IMonitorContext monitor, Guid nodeOrTemplateId)
        {
            _SettingsInstancesCache.Clear();

            return _EvaluateNode(monitor, nodeOrTemplateId, true);
        }

        private Object _EvaluateNode(SDK.IMonitorContext monitor, Guid nodeOrTemplateId, bool previewMode, params Object[] parameters)
        {
            if (monitor != null && monitor.IsCancelRequested) throw new OperationCanceledException();

            // First, we check if it's a template, in which case we return it as the evaluated value (it will be called by the component)
            var pipelineEvaluator = _PipelineInstances.GetValueOrDefault(nodeOrTemplateId);
            if (pipelineEvaluator != null) return pipelineEvaluator;

            // Get the current node being evaluated
            var nodeInst = _NodeInstances.GetValueOrDefault(nodeOrTemplateId);
            if (nodeInst == null) return null;
            if (nodeInst is _UnknownNode) return null;

            // Next, we try to find the property values for this node
            var nodeProps = _Pipeline
                .GetNode(nodeOrTemplateId)?
                .GetPropertiesForConfiguration(_BuildSettings.Configuration)
                .AsReadOnly();
            if (nodeProps == null) return null;

            // Evaluate values and dependencies. Dependecies are evaluated recursively
            nodeInst.EvaluateBindings(nodeProps,xid => _EvaluateNode(monitor, xid, previewMode,parameters));

            // AFTER evaluating the bindings, inject template parameters, if applicable.            
            if (_Template != null)
            {
                var templateArgTypes = GetTemplateParameterTypes().ToArray();
                var nodeBindings = nodeInst.CreateBindings(nodeProps);

                for (int i = 0; i < parameters.Length; ++i)
                {
                    var p = _Template.Parameters.Skip(i).FirstOrDefault();
                    if (p == null) break;
                    if (p.NodeId != nodeOrTemplateId) continue;

                    var templatedBinding = nodeBindings
                        .OfType<Bindings.ValueBinding>()
                        .FirstOrDefault(item => item.SerializationKey == p.NodeProperty);                    

                    if (templatedBinding != null)
                    {
                        // check if argument types match

                        var pType = templateArgTypes[i];

                        bool areCompatible = templatedBinding.DataType.GetTypeInfo().IsAssignableFrom(pType);

                        templatedBinding.SetEvaluatedResult(parameters[i]);
                    }
                }
            }

            if (monitor != null && monitor.IsCancelRequested) throw new OperationCanceledException();

            // evaluate the current node            

            try
            {
                var localMonitor = monitor == null ? null : monitor.GetProgressPart(_NodeOrder.IndexOf(nodeOrTemplateId), _NodeOrder.Count);

                Func<Type, SDK.ContentObject> sharedContentEvaluator = t => GetSettingsInstance(t, localMonitor);

                if (nodeInst is SDK.ContentFilter)
                {
                    if (previewMode) return SDK.PreviewNode((SDK.ContentFilter)nodeInst, localMonitor, sharedContentEvaluator);
                    else
                    {
                        if (System.Diagnostics.Debugger.IsAttached) return SDK.DebugNode((SDK.ContentFilter)nodeInst, localMonitor, sharedContentEvaluator);
                        else return SDK.EvaluateNode((SDK.ContentFilter)nodeInst, localMonitor, sharedContentEvaluator);
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

        #endregion

        #region API - Template

        public string[] TemplateParameters { get { return _Template?.Parameters.Select(item => item.BindingName).ToArray(); } }

        public IEnumerable<Type> GetTemplateParameterTypes()
        {
            if (_Template == null) yield break;

            foreach (var p in _Template.Parameters.ExceptNulls())
            {
                var nodeDom = _Pipeline.GetNode(p.NodeId); if (nodeDom == null) yield return null;
                if (!_NodeInstances.TryGetValue(p.NodeId, out var nodeInst)) yield return null;

                var nodeBinding = nodeInst
                    .CreateBindings(null)
                    .FirstOrDefault(item => item.SerializationKey == p.NodeProperty);

                if (nodeBinding == null) yield return null;

                yield return nodeBinding.DataType;
            }
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