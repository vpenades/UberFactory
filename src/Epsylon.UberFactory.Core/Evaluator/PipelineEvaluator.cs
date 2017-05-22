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

        private readonly ProjectDOM.Template _Template; // optional, it can be null
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
            // find node            
            var nodeDom = _Pipeline.GetNode(nodeId);
            if (nodeDom == null) return;            

            // create instance
            var nodeInst = _PluginFactory(nodeDom.ClassIdentifier, _BuildSettings);
            if (nodeInst == null) throw new NullReferenceException("Couldn't create Node instance for ClassID: " + nodeDom.ClassIdentifier);

            var nodeDesc = Factory.ContentFilterTypeInfo.Create(nodeInst);
            if (nodeDesc == null) throw new NullReferenceException("not a node type: " + nodeDom.ClassIdentifier);

            _NodeInstances[nodeId] = nodeInst;

            
            // retrieve property values from current cunfiguration
            var properties = nodeDom.GetPropertiesForConfiguration(_BuildSettings.Configuration);

            // bind property values to instance
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


        public SDK.IPipelineInstance SetArgument(string name, object value)
        {
            // this is not working, because it's setting the properties of the instance in the wrong time.
            // maybe we need to create an "overlay" of the property group that overrides everything else

            if (_Template == null) throw new InvalidOperationException("Not a template");

            var tb = _Template.GetParameterByBindingName(name);

            var targetNode = _NodeInstances
                .GetValueOrDefault(tb.NodeId);

            var targetBind = targetNode
                .CreateBindings(null)
                .OfType<Bindings.ValueBinding>()
                .FirstOrDefault(item => item.SerializationKey == tb.NodeProperty);
            
            targetBind.SetEvaluatedResult(value);

            return this;
        }

        public Object Evaluate(params Object[] parameters)
        {
            if (_Template != null)
            {
                for(int i=0; i < parameters.Length; ++i)
                {
                    var p = _Template.Parameters.Skip(i).FirstOrDefault();
                    if (p == null) break;

                    SetArgument(p.BindingName, parameters[i]);
                }                
            }            

            var rootInstance = _NodeInstances.GetValueOrDefault(_Pipeline.RootIdentifier);

            return Evaluate(_Pipeline.RootIdentifier);
        }        

        public Object Evaluate(Guid nodeOrTemplateId, bool previewMode = false)
        {
            if (_Monitor.Cancelator.IsCancellationRequested) throw new OperationCanceledException();

            // first, we check if it's a template, and if it is, we return it as the evaluated value (it will be called by the component)
            var pipelineEvaluator = _PipelineInstances.GetValueOrDefault(nodeOrTemplateId);
            if (pipelineEvaluator != null) return pipelineEvaluator;

            // next, we try to find the component
            var nodeProps = _Pipeline.GetNode(nodeOrTemplateId)?.GetPropertiesForConfiguration(_BuildSettings.Configuration);
            if (nodeProps == null) return null;
            var nodeInst = _NodeInstances.GetValueOrDefault(nodeOrTemplateId);
            if (nodeInst == null) return null;

            // here we can create a wrapper to nodeProps with the values of the template,
            // so they're used by the bindings by default

            // evaluate values returned by node dependencies recursively
            nodeInst.EvaluateBindings(nodeProps,xid => Evaluate(xid) );

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
