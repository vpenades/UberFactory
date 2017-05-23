using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;



namespace Epsylon.UberFactory
{
    using TEMPLATEFACTORY = Func<Guid, ProjectDOM.Template>;
    using FILTERFACTORY = Func<String, BuildContext, SDK.ContentFilter>;

    public class PipelineEvaluator : SDK.IPipelineInstance
    {
        #region lifecycle

        public struct Monitor : IProgress<float>
        {
            #region lifecycle

            public static Monitor Create(System.Threading.CancellationToken cancelToken, IProgress<float> progressAgent)
            {
                return new Monitor()
                {
                    Cancelator = cancelToken,
                    _Progress = progressAgent
                };
            }

            public Monitor CreatePart(int part, int total)
            {
                return new Monitor()
                {
                    Cancelator = this.Cancelator,
                    _Progress = this._Progress.CreatePart(part, total)
                };
            }

            #endregion

            #region data

            public static readonly Monitor Empty = Create(System.Threading.CancellationToken.None, null);            

            public System.Threading.CancellationToken Cancelator;
            private IProgress<float> _Progress;            

            #endregion

            #region API

            public void Report(float value)
            {
                if (_Progress == null) return;
                _Progress.Report(value.Clamp(0, 1));
            }

            #endregion
        }

        public static PipelineEvaluator CreatePipelineInstance(ProjectDOM.Pipeline pipeline, TEMPLATEFACTORY templateResolver, FILTERFACTORY pluginResolver, Monitor monitor)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (pluginResolver == null) throw new ArgumentNullException(nameof(pluginResolver));
            if (templateResolver == null) throw new ArgumentNullException(nameof(templateResolver));            

            return new PipelineEvaluator(pipeline, templateResolver, pluginResolver, monitor);
        }

        public static PipelineEvaluator CreatePipelineInstance(ProjectDOM.Template template, TEMPLATEFACTORY templateResolver, FILTERFACTORY pluginResolver, Monitor monitor)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (template.Pipeline == null) throw new ArgumentNullException(nameof(template));
            if (pluginResolver == null) throw new ArgumentNullException(nameof(pluginResolver));
            if (templateResolver == null) throw new ArgumentNullException(nameof(templateResolver));

            return new PipelineEvaluator(template, templateResolver, pluginResolver, monitor);
        }

        private PipelineEvaluator(ProjectDOM.Pipeline pipeline, TEMPLATEFACTORY templateResolver, FILTERFACTORY plugins, Monitor monitor)
        {
            _PluginFactory = plugins;
            _TemplateFactory = templateResolver;

            _Template = null;
            _Pipeline = pipeline;

            _Monitor = monitor;            
        }

        private PipelineEvaluator(ProjectDOM.Template template, TEMPLATEFACTORY templateResolver, FILTERFACTORY plugins, Monitor monitor)
        {
            _PluginFactory = plugins;
            _TemplateFactory = templateResolver;

            _Template = template;
            _Pipeline = template.Pipeline;

            _Monitor = monitor;
        }

        #endregion

        #region data

        private readonly Monitor _Monitor;        

        private readonly FILTERFACTORY _PluginFactory;
        private readonly TEMPLATEFACTORY _TemplateFactory;

        private readonly ProjectDOM.Template _Template; // Non Null if this evaluator is a template
        private readonly ProjectDOM.Pipeline _Pipeline; // here is the serialized data from where we initialize the instances        
        
        // evaluation data

        private BuildContext _BuildSettings;        
        private readonly Dictionary<Guid, SDK.ContentFilter> _NodeInstances = new Dictionary<Guid, SDK.ContentFilter>();
        private readonly Dictionary<Guid, PipelineEvaluator> _PipelineInstances = new Dictionary<Guid, PipelineEvaluator>();

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
            _PipelineInstances.Clear();            

            _CreateNodeInstancesRecursive(_Pipeline.RootIdentifier);
        }

        /// <summary>
        /// Creates a filter instance for a given node ID
        /// </summary>
        /// <remarks>
        /// Creates the instace for the current ID, and resolves the dependencies of the whole tree.
        /// </remarks>
        /// <param name="nodeId">root node id</param>
        private void _CreateNodeInstancesRecursive(Guid nodeId)
        {
            // Find node DOM
            var nodeDom = _Pipeline.GetNode(nodeId);
            if (nodeDom == null) return;            

            // Create node instance
            var nodeInst = _PluginFactory(nodeDom.ClassIdentifier, _BuildSettings);
            if (nodeInst == null) throw new NullReferenceException("Couldn't create Node instance for ClassID: " + nodeDom.ClassIdentifier);            

            _NodeInstances[nodeId] = nodeInst;
            
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
                        var templateInst = CreatePipelineInstance(templateDom, _TemplateFactory, _PluginFactory, new Monitor());
                        templateInst.Setup(_BuildSettings);

                        _PipelineInstances[templateId] = templateInst;
                    }
                }

                if (binding is Bindings.SingleDependencyBinding)
                {
                    var id = ((Bindings.SingleDependencyBinding)binding).GetDependency();

                    _CreateNodeInstancesRecursive(id);
                }

                if (binding is Bindings.MultiDependencyBinding)
                {
                    var ids = ((Bindings.MultiDependencyBinding)binding).GetDependencies();

                    foreach(var id in ids) _CreateNodeInstancesRecursive(id);
                }
            }            
        }        

        #endregion

        #region API - Evaluation

        public SDK.ContentFilter GetNodeInstance(Guid id) { return _NodeInstances.GetValueOrDefault(id); }                

        public IEnumerable<Bindings.MemberBinding> CreateBindings(Guid nodeId)
        {
            // get source and destination objects            
            var nodeProps = _Pipeline.GetNode(nodeId)?.GetPropertiesForConfiguration(_BuildSettings.Configuration);
            var nodeInst = _NodeInstances.GetValueOrDefault(nodeId);            

            // evaluate only the values
            nodeInst.EvaluateBindings(nodeProps, null); // we don't pass the dependency evaluator callback because we only want to assign the initial values, we don't want a full chain evaluation

            return nodeInst.CreateBindings(nodeProps);
        }
        
        /// <summary>
        /// called from a template evaluator
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Object Evaluate(params Object[] parameters)
        {
            var rootInstance = _NodeInstances.GetValueOrDefault(_Pipeline.RootIdentifier);

            return Evaluate(_Pipeline.RootIdentifier, false, parameters);
        }        

        public Object Evaluate(Guid nodeOrTemplateId, bool previewMode, params Object[] parameters)
        {
            if (_Monitor.Cancelator.IsCancellationRequested) throw new OperationCanceledException();

            // First, we check if it's a template, in which case we return it as the evaluated value (it will be called by the component)
            var pipelineEvaluator = _PipelineInstances.GetValueOrDefault(nodeOrTemplateId);
            if (pipelineEvaluator != null) return pipelineEvaluator;

            // Get the current node being evaluated
            var nodeInst = _NodeInstances.GetValueOrDefault(nodeOrTemplateId);
            if (nodeInst == null) return null;

            // Next, we try to find the property values for this node
            var nodeProps = _Pipeline
                .GetNode(nodeOrTemplateId)?
                .GetPropertiesForConfiguration(_BuildSettings.Configuration)
                .AsReadOnly();
            if (nodeProps == null) return null;

            // Evaluate values and dependencies. Dependecies are evaluated recursively
            nodeInst.EvaluateBindings(nodeProps,xid =>  Evaluate(xid,previewMode,parameters));

            // AFTER evaluating the bindings, inject template parameters, if applicable.
            
            if (_Template != null)
            {
                var nodeBindings = nodeInst.CreateBindings(nodeProps);

                for (int i = 0; i < parameters.Length; ++i)
                {
                    var p = _Template.Parameters.Skip(i).FirstOrDefault();
                    if (p == null) break;
                    if (p.NodeId != nodeOrTemplateId) continue;


                    var templatedBinding = nodeBindings
                        .OfType<Bindings.ValueBinding>()
                        .FirstOrDefault(item => item.SerializationKey == p.NodeProperty);

                    if (templatedBinding != null) templatedBinding.SetEvaluatedResult(parameters[i]);                    
                }
            }

            if (_Monitor.Cancelator.IsCancellationRequested) throw new OperationCanceledException();

            // evaluate the current node
            // var localMonitor = _Monitor.CreatePart(progressPart, _NodeInstances.Count);

            try
            {
                if (nodeInst is SDK.ContentFilter)
                {
                    if (previewMode) return SDK.PreviewNode((SDK.ContentFilter)nodeInst, _Monitor.Cancelator, _Monitor);
                    else
                    {
                        if (System.Diagnostics.Debugger.IsAttached) return SDK.DebugNode((SDK.ContentFilter)nodeInst, _Monitor.Cancelator, _Monitor);
                        else return SDK.EvaluateNode((SDK.ContentFilter)nodeInst, _Monitor.Cancelator, _Monitor);
                    }
                }
            }
            catch(Exception ex)
            {
                throw new PluginException(ex);
            }

            throw new NotImplementedException();
        }

        #endregion       
    }


    [SDK.ContentFilter("PLUGIN ERROR")]
    class _UnknownNode : SDK.ContentFilter
    {
        protected override object EvaluateObject()
        {
            return null;
        }
    }
}
