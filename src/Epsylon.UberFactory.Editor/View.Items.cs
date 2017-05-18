using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Epsylon.UberFactory
{
    public static partial class ProjectVIEW
    {
        internal sealed class NULL_Type { private NULL_Type() { } }

        public class Task : BindableBase, IPipelineViewServices
        {
            #region lifecycle

            public static Task Create(Project d, ProjectDOM.Task c)
            {
                if (d == null || c == null) return null;

                return new Task(d, c);
            }

            private Task(Project d, ProjectDOM.Task c)
            {
                _Parent = d;
                _Source = c;
            }

            #endregion

            #region data

            private readonly Project _Parent;
            private readonly ProjectDOM.Task _Source;

            private Pipeline _PipelineView;

            #endregion

            #region properties

            public ProjectDOM.Task  Source          => _Source;

            public Project          ParentProject   => _Parent;            

            public String           InferredTitle   => _PipelineView?._Evaluator.InferredTitle;

            public String           DisplayTitle    => "Task " + Title;

            public Boolean Enabled { get { return _Source.Enabled; } set { _Source.Enabled = value; } }

            public String Title
            {
                get { return _Source.Title; }
                set { _Source.Title = value; RaiseChanged(nameof(Title), nameof(DisplayTitle)); }
            }

            public Pipeline Pipeline
            {
                get
                {
                    if (_PipelineView == null) _PipelineView = Pipeline.Create(this, _Source.Pipeline);
                    return _PipelineView;
                }
            }

            public IEnumerable<Type> AvailableReturnTypes=> Enumerable.Empty<Type>();

            public Type ActiveReturnType { get { return null; } set { } }

            #endregion

            #region API - Pipeline services

            public PluginManager GetPluginManager() { return _Parent._Plugins; }

            public BuildContext GetBuildSettings() { return _Parent.GetBuildSettings(); }

            public bool AllowTemplateEdition { get { return false; } }

            public ProjectDOM.Template GetTemplate(Guid id)
            {
                return _Parent.Templates.FirstOrDefault(item => item.Source.Identifier == id)?.Source;
            }

            public IEnumerable<ProjectDOM.Template> GetTemplates() { return _Parent.Templates.Select(item => item.Source); }

            public Type GetRootOutputType() { return null; }

            #endregion
        }

        public class Template : BindableBase, IPipelineViewServices
        {
            #region lifecycle

            public static Template Create(Project d, ProjectDOM.Template t)
            {
                if (d == null || t == null) return null;

                return new Template(d, t);
            }

            private Template(Project d, ProjectDOM.Template t)
            {
                _Parent = d;
                _Source = t;
            }

            #endregion

            #region data

            private readonly Project _Parent;
            private readonly ProjectDOM.Template _Source;

            private Pipeline _PipelineView;
            
            private Type _ActiveReturnType = typeof(NULL_Type);

            #endregion

            #region properties

            public ProjectDOM.Template Source   => _Source;

            public Project ParentProject        => _Parent;

            public string Title { get { return _Source.Title; } set { _Source.Title = value; RaiseChanged(nameof(Title), nameof(DisplayTitle)); } }

            public String DisplayTitle          => "Template " + Title;

            public Pipeline Pipeline
            {
                get
                {
                    if (_PipelineView == null) _PipelineView = Pipeline.Create(this, _Source.Pipeline);
                    return _PipelineView;
                }
            }

            public IEnumerable<TemplateParameter> Parameters
            {
                get
                {
                    return _Source
                        .Parameters
                        .Select(item => new TemplateParameter(Pipeline, item, RemoveTemplateParameter))
                        .Concat(new TemplateParameter[] { new TemplateParameter(Pipeline, null, xnull=> AddTemplateParameter()) })
                        .ToArray();
                }
            }

            public IEnumerable<Type> AvailableReturnTypes
            {
                get
                {
                    return GetPluginManager()
                        .PluginTypes
                        .OfType<Factory.ContentFilterTypeInfo>()
                        .Select(item => item.OutputType)
                        .ExceptNulls()
                        .Distinct()                        
                        .OrderBy(item => item.Name)
                        .Concat(new Type[] { typeof(NULL_Type) })  // add special type used to declare null types
                        .ToArray();
                }
            }

            public Type ActiveReturnType
            {
                get { return _ActiveReturnType; }
                set { _ActiveReturnType = value; RaiseChanged(nameof(ActiveReturnType)); }
            }

            #endregion

            #region API - Pipeline services

            public PluginManager GetPluginManager() { return _Parent._Plugins; }

            public BuildContext GetBuildSettings() { return _Parent.GetBuildSettings(); }

            public bool AllowTemplateEdition { get { return true; } }

            public void AddTemplateParameter()
            {
                _Source.AddNewParameter();
                RaiseChanged(nameof(Parameters));
            }

            public void RemoveTemplateParameter(ProjectDOM.TemplateParameter param)
            {
                _Source.RemoveParameter(param);
                RaiseChanged(nameof(Parameters));
            }

            public ProjectDOM.Template GetTemplate(Guid id)
            {
                return _Parent.Templates.FirstOrDefault(item => item.Source.Identifier == id)?.Source;
            }

            public IEnumerable<ProjectDOM.Template> GetTemplates()
            {
                // TODO: remove self to avoid circular

                return _Parent.Templates.Select(item => item.Source);
            }

            public Type GetRootOutputType()
            {
                if (_ActiveReturnType == typeof(NULL_Type)) return null;
                return _ActiveReturnType;
            }

            #endregion
        }

        public class TemplateParameter : BindableBase
        {
            #region lifecycle

            internal TemplateParameter(Pipeline p, ProjectDOM.TemplateParameter tp, Action<ProjectDOM.TemplateParameter> addOrRemove)
            {
                _Pipeline = p;
                _Param = tp;

                _AllNodes = tp == null ? null : p.Nodes.Where(item => !string.IsNullOrWhiteSpace(item.TemplateName)).ToArray();

                AddOrRemoveCmd = new RelayCommand(() => addOrRemove(_Param));
            }

            #endregion

            #region data

            public ICommand AddOrRemoveCmd { get; private set; }

            private readonly Pipeline _Pipeline;
            private readonly ProjectDOM.TemplateParameter _Param;

            private readonly Node[] _AllNodes;

            private Bindings.ValueBinding[] _ActiveBindings;

            #endregion

            #region properties

            public bool IsActive                        => _Param != null;

            public bool IsEmpty                         => !IsActive;            

            public Node[] AllNodes                      => _AllNodes;

            


            public String ParameterName
            {
                get { return IsEmpty ? null : _Param.BindingName; }
                set { _Param.BindingName = value;  RaiseChanged(nameof(ParameterName)); }
            }            

            public Node ActiveNode
            {
                get { return IsEmpty ? null : _AllNodes.FirstOrDefault(item => item.Id == _Param.NodeId); }
                set
                {
                    _Param.NodeId = value == null ? Guid.Empty : value.Id;

                    _ActiveBindings = null;

                    RaiseChanged(nameof(ActiveNode), nameof(AllBindings), nameof(ActiveBinding));
                }
            }

            public Bindings.ValueBinding[] AllBindings
            {
                get
                {
                    if (_ActiveBindings == null) _ActiveBindings = ActiveNode?.Bindings.OfType<Bindings.ValueBinding>().ToArray();
                    return _ActiveBindings;
                }
            }

            public Bindings.ValueBinding ActiveBinding
            {
                get { return _ActiveBindings?.FirstOrDefault(item => item.SerializationKey == _Param.NodeProperty); }
                set { _Param.NodeProperty = value?.SerializationKey; RaiseChanged(nameof(ActiveBinding)); }
            }            

            #endregion
        }

    }
}
