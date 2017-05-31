﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Epsylon.UberFactory
{
    using Epsylon.UberFactory.Bindings;
    using BINDING = System.ComponentModel.INotifyPropertyChanged;

    public static partial class ProjectVIEW
    {
        public interface INodeViewFactory
        {
            #region read

            ProjectDOM.Node GetNodeDOM(Guid id);

            String GetNodeDisplayName(Guid id);

            String GetNodeDisplayFormat(Guid id);                        

            IEnumerable<Bindings.MemberBinding> CreateNodeBindings(Guid id);

            #endregion

            #region write

            IPipelineViewServices PipelineServices { get; }

            Boolean CanEditHierarchy { get; }

            void SetAsCurrentResultView(Guid id); // EvaluatePreview();

            Guid AddNode(Factory.ContentBaseTypeInfo cbinfo);

            void UpdateGraph();

            #endregion
        }
        
        
        public interface IPipelineViewServices
        {
            bool AllowTemplateEdition { get; }

            PluginManager GetPluginManager();
            BuildContext GetBuildSettings();

            ProjectDOM.Template GetTemplate(Guid id);

            IEnumerable<ProjectDOM.Template> GetTemplates();

            Type GetRootOutputType();
        }



        public class SettingsView : BindableBase , INodeViewFactory
        {
            #region lifecycle

            public static SettingsView Create(IPipelineViewServices c, ProjectDOM.Settings s)
            {
                if (c == null) return null;
                if (s == null) return null;

                var pp = new SettingsView(c, s);

                // pp.UpdateGraph();

                return pp;
            }

            private SettingsView(IPipelineViewServices c, ProjectDOM.Settings s)
            {
                _Parent = c;
                _SettingsDom = s;
            }

            #endregion

            #region data

            internal readonly IPipelineViewServices _Parent;
            internal readonly ProjectDOM.Settings _SettingsDom;

            #endregion

            #region properties

            public IPipelineViewServices PipelineServices => _Parent;

            public bool CanEditHierarchy => false;

            public Node[] Nodes => _SettingsDom?.Nodes.Select(f => Node.Create(this, f.Identifier)).ToArray();

            #endregion

            #region API

            public ProjectDOM.Node GetNodeDOM(Guid id) { return _SettingsDom.Nodes.FirstOrDefault(item => item.Identifier == id); }

            public string GetNodeDisplayName(Guid id)
            {
                // temporary solution; do the correct thing
                return GetNodeDOM(id).ClassIdentifier;
            }

            public string GetNodeDisplayFormat(Guid id)
            {
                // temporary solution; do the correct thing
                return GetNodeDOM(id).ClassIdentifier;
            }

            public IEnumerable<MemberBinding> CreateNodeBindings(Guid id)
            {                
                var nodeDOM = GetNodeDOM(id);
                var bsettings = _Parent.GetBuildSettings();
                var nodeINS = _Parent.GetPluginManager().CreateGlobalSettingsInstance(nodeDOM.ClassIdentifier, bsettings);

                var props = nodeDOM.GetPropertiesForConfiguration(bsettings.Configuration);

                return nodeINS.CreateBindings(props)
                    .OfType<Bindings.ValueBinding>()
                    .ToArray();                
            }

            public void SetAsCurrentResultView(Guid id) { throw new NotSupportedException(); }

            public Guid AddNode(Factory.ContentBaseTypeInfo cbinfo) { throw new NotSupportedException(); }

            public void UpdateGraph() { throw new NotSupportedException(); }            

            #endregion
        }



        public class Pipeline : BindableBase , INodeViewFactory
        {
            #region lifecycle

            public static Pipeline Create(IPipelineViewServices c, ProjectDOM.Pipeline p)
            {
                if (c == null) return null;
                if (p == null) return null;

                var pp = new Pipeline(c, p);

                pp.UpdateGraph();

                return pp;
            }

            private Pipeline(IPipelineViewServices c, ProjectDOM.Pipeline p)
            {
                _Parent = c;
                _PipelineDom = p;

                ClearCmd = new RelayCommand(ClearAll);

                SetRootNodeCmd = new RelayCommand(_SetRootNode);
            }

            #endregion

            #region commands

            public ICommand ClearCmd { get; private set; }

            public ICommand SetRootNodeCmd { get; private set; }            

            #endregion

            #region data

            internal readonly IPipelineViewServices _Parent;
            internal readonly ProjectDOM.Pipeline _PipelineDom;

            internal PipelineEvaluator _Evaluator;

            private Exception _Exception;

            #endregion

            #region properties            

            public Node[] Nodes         => _PipelineDom?.Nodes.Select(f => Node.Create(this, f.Identifier)).ToArray();

            public bool IsEmpty         => _PipelineDom?.Nodes.Count == 0 && _Exception == null;            

            public bool IsInstanced     => !IsEmpty;            

            public Object Content       => _Exception != null ? (Object)_Exception : (Object)Node.Create(this, _PipelineDom.RootIdentifier);

            public Boolean CanEditHierarchy => _Parent.GetBuildSettings().Configuration.Length == 1;

            public bool AllowTemplateEdition => throw new NotImplementedException();

            public IPipelineViewServices PipelineServices => _Parent;

            #endregion

            #region API

            public void UpdateGraph()
            {
                // note: if the general document configuration changes, we must call setup again              
                
                try
                { 
                    var evaluator = PipelineEvaluator.CreatePipelineInstance(_PipelineDom, _Parent.GetTemplate , _Parent.GetPluginManager().CreateContentFilterInstance);
                    evaluator.Setup(_Parent.GetBuildSettings());
                    _Evaluator = evaluator;
                    _Exception = null;

                }
                catch (Exception ex)
                {
                    _Exception = ex;
                    _Evaluator = null;
                }

                RaiseChanged();                
            }            

            public void ClearAll()
            {
                _PipelineDom.ClearNodes();
                UpdateGraph();
            }

            private void _SetRootNode()
            {
                var compatibleNodes = _Parent
                    .GetPluginManager()
                    .PluginTypes
                    .OfType<Factory.ContentFilterTypeInfo>()
                    .Where(item => item.OutputType == _Parent.GetRootOutputType())
                    .ToArray();                          

                var r = _Dialogs.ShowNewNodeDialog(null, compatibleNodes);
                if (r == null) return;
                _SetRootExporter(r);
            }

            private void _SetRootExporter(Factory.ContentFilterTypeInfo t)
            {
                _PipelineDom.ClearNodes();
                _PipelineDom.RootIdentifier = _PipelineDom.AddNode(t);
                UpdateGraph();
            }

            public void SetAsCurrentResultView(Guid nodeId)
            {
                if (_Evaluator == null) return;
                var result = _Evaluator.EvaluateNode(MonitorContext.CreateNull(), nodeId,true);

                _Dialogs.ShowProductAndDispose(null, result);                
            }

            public ProjectDOM.Node GetNodeDOM(Guid id) { return _PipelineDom.GetNode(id); }

            public string GetNodeDisplayName(Guid id)
            {
                var inst = _Evaluator.GetNodeInstance(id);
                return Factory.ContentFilterTypeInfo.Create(inst).DisplayName;
            }

            public string GetNodeDisplayFormat(Guid id)
            {
                var inst = _Evaluator.GetNodeInstance(id);
                return Factory.ContentFilterTypeInfo.Create(inst).DisplayFormatName;
            }

            public IEnumerable<Bindings.MemberBinding> CreateNodeBindings(Guid id)
            {
                return _Evaluator.CreateBindings(id);
            }

            public Guid AddNode(Factory.ContentBaseTypeInfo cbinfo)
            {
                return _PipelineDom.AddNode(cbinfo);
            }

            #endregion
        }

        public class Node : BindableBase
        {
            #region lifecycle

            public static Node Create(INodeViewFactory p, Guid nodeId)
            {
                if (p == null) return null;                
                if (nodeId == Guid.Empty) return null;
                if (p.GetNodeDOM(nodeId) == null) return null;                

                var node = new Node(p, nodeId);                

                var propertyBindings = p.CreateNodeBindings(nodeId);

                // now we need to wrap some bindings with UI friendly wrappers
                var propertyViews = propertyBindings.Cast<BINDING>().ToArray();
                for (int i = 0; i < propertyViews.Length; ++i)
                {
                    var bv = propertyViews[i];

                    if (bv is Bindings.PipelineDependencyBinding) propertyViews[i] = PipelineDependencyView._Create(node, (Bindings.PipelineDependencyBinding)bv);
                    if (bv is Bindings.MultiDependencyBinding) propertyViews[i] = ArrayDependencyView._Create(node, (Bindings.MultiDependencyBinding)bv);
                    if (bv is Bindings.SingleDependencyBinding) propertyViews[i] = SingleDependencyView._Create(node, (Bindings.SingleDependencyBinding)bv);                    
                }

                node._BindingsViews = propertyViews;

                return node;                
            }

            private Node(INodeViewFactory p, Guid nodeId)
            {
                System.Diagnostics.Debug.Assert(p.GetNodeDOM(nodeId) != null);

                _Parent = p;
                _NodeId = nodeId;                
            }

            #endregion

            #region data

            private readonly INodeViewFactory _Parent;
            private readonly Guid _NodeId;
            private BINDING[] _BindingsViews;

            #endregion

            #region properties

            public Guid Id                          => _NodeId;

            public INodeViewFactory Parent          => _Parent;

            public ProjectDOM.Node NodeDescription  => _Parent.GetNodeDOM(_NodeId);

            // public SDK.ContentFilter NodeInstance   => _Parent._Evaluator.GetNodeInstance(_NodeId);            

            public string DisplayName               => _Parent.GetNodeDisplayName(_NodeId);

            public string PropertyFormatName        => _Parent.GetNodeDisplayFormat(_NodeId);

            public IEnumerable<BINDING> Bindings    => _BindingsViews;

            public IEnumerable<BINDING> BindingsGrouped => GroupedBindingsView.Group(_BindingsViews);

            public bool AllowTemplateEdition        => _Parent.PipelineServices.AllowTemplateEdition;

            public string TemplateName { get { return NodeDescription.TemplateIdentifier; } set { NodeDescription.TemplateIdentifier = value; } }

            #endregion

            #region API            

            public void SetAsCurrentResultView() { _Parent.SetAsCurrentResultView(_NodeId); }
            
            #endregion
        }


        public class GroupedBindingsView : BindableBase
        {
            public static IEnumerable<BINDING> Group(IEnumerable<BINDING> bindings)
            {
                var r = new List<BINDING>();

                foreach(var b in bindings)
                {
                    var vb = b as Bindings.ValueBinding;
                    if (vb == null) { r.Add(b); continue; }

                    var gn = vb.GroupName; if (string.IsNullOrWhiteSpace(gn)) { r.Add(b); continue; }

                    var g = r.OfType<GroupedBindingsView>().FirstOrDefault(item => item.DisplayName == gn);
                    if (g == null) { g = new GroupedBindingsView(gn); r.Add(g); }

                    g._BindingsViews.Add(b);                    
                }

                return r;
            }

            private GroupedBindingsView(string displayName) { _DisplayName = displayName; }

            private readonly string _DisplayName;

            private readonly List<BINDING> _BindingsViews = new List<BINDING>();

            public string DisplayName => _DisplayName;

            public IEnumerable<BINDING> Bindings => _BindingsViews.ToArray();
        }


        public class PipelineDependencyView : BindableBase
        {
            // handles the binding of a single node dependency;
            // this view class has a dual mode function:
            // if _Binding is a SingleDependencyBinding, it defines the binding of a single dependency with its node.
            // if _Binding is a MultiDependencyBinding, it defines the binding of a single, indexed, dependency with an item of the array of _MultiDependencyBinding.

            #region lifecycle            

            internal static PipelineDependencyView _Create(Node parent, Bindings.PipelineDependencyBinding binding)
            {
                if (parent == null) return null;
                if (binding == null) return null;

                return new PipelineDependencyView(parent, binding);
            }            

            private PipelineDependencyView(Node parent, Bindings.PipelineDependencyBinding binding)
            {
                _Parent = parent;
                _Binding = binding;
                

                ChooseBindingCmd = new RelayCommand(_SetNewTemplate);
                RemoveBindingCmd = new RelayCommand(_RemoveTemplate);

                // ViewResultCmd = new RelayCommand(() => { var view = NodeInstance; if (view != null) view.SetAsCurrentResultView(); });
            }

            #endregion

            #region commands

            public ICommand ChooseBindingCmd { get; private set; }

            public ICommand RemoveBindingCmd { get; private set; }

            public ICommand ViewResultCmd { get; private set; }

            #endregion

            #region data

            private readonly Node _Parent;

            private readonly Bindings.PipelineDependencyBinding _Binding;            

            #endregion

            #region properties

            public string DisplayName                   => _Binding.DisplayName + " " + TemplateDom?.Title;            

            public string TitleFormat                   => DisplayName + "  {0}"; // formatting used to combine the name of the property and the template

            public ProjectDOM.Template TemplateDom      => _Parent.Parent.PipelineServices.GetTemplate(_GetDependencyId());

            public bool IsInstanced                     => TemplateDom != null;

            public bool IsEmpty                         => !IsInstanced;            

            public Type DataType                        => typeof(SDK.IPipelineInstance);

            #endregion

            #region API

            private void _RemoveTemplate() { _SetDependencyId(Guid.Empty); }
            
            private void _SetNewTemplate()
            {
                
                var templateSignature = _Binding.GetTemplateSignature();

                var templateDOMs = _Parent.Parent.PipelineServices.GetTemplates();
                //var templateINSs = templateDOMs.Select(item => PipelineEvaluator.CreatePipelineInstance(item))

                // TODO: foreach templateDOM, create a pipeline evaluator, so we can retrieve the types and filter the compatible types

                var r = _Dialogs.ShowNewTemplateDialog(null, templateDOMs);
                if (r != null) _SetDependencyId(r.Identifier);
            }            

            private Guid _GetDependencyId() { return _Binding.GetDependency(); }

            private void _SetDependencyId(Guid templateId) { _Binding.SetDependency(templateId); RaiseChanged(); }

            #endregion
        }



        public class SingleDependencyView : BindableBase
        {
            // handles the binding of a single node dependency;
            // this view class has a dual mode function:
            // if _Binding is a SingleDependencyBinding, it defines the binding of a single dependency with its node.
            // if _Binding is a MultiDependencyBinding, it defines the binding of a single, indexed, dependency with an item of the array of _MultiDependencyBinding.

            #region lifecycle            

            internal static SingleDependencyView _Create(Node parent, Bindings.SingleDependencyBinding binding)
            {
                if (parent == null) return null;
                if (binding == null) return null;                

                return new SingleDependencyView(parent, binding, -1);
            }

            internal static SingleDependencyView _Create(Node parent, Bindings.MultiDependencyBinding binding, int index)
            {                
                if (parent == null) return null;
                if (binding == null) return null;
                if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));                

                return new SingleDependencyView(parent, binding, index);
            }

            private SingleDependencyView(Node parent, Bindings.DependencyBinding binding, int index)
            {
                _Parent = parent;
                _Binding = binding;
                _Index = index;

                ChooseParameterCmd = new RelayCommand(_SetNewDependencyNode);                
                RemoveParameterCmd = new RelayCommand(_RemoveParameter);
                ViewResultCmd = new RelayCommand(() => { var view = NodeInstance; if (view != null) view.SetAsCurrentResultView(); });
                if (_Index >=0) RemoveElementCmd = new RelayCommand(_RemoveElement);
            }

            #endregion

            #region commands

            public ICommand ChooseParameterCmd { get; private set; }            

            public ICommand RemoveParameterCmd { get; private set; }

            public ICommand ViewResultCmd { get; private set; }

            public ICommand RemoveElementCmd { get; private set; }

            #endregion

            #region data

            private readonly Node _Parent;

            private readonly Bindings.DependencyBinding _Binding;
            private readonly int _Index;            

            #endregion

            #region properties

            public string DisplayName
            {
                get
                {
                    var propertyName = _Index < 0 ? _Binding.DisplayName : _Index.ToString();

                    if (IsEmpty) return propertyName;

                    return string.Format(NodeInstance.PropertyFormatName, propertyName);
                }
                
            }            

            public Node NodeInstance    => Node.Create(_Parent.Parent, _GetDependencyId());

            public bool IsEmpty         => NodeInstance == null;

            public bool IsInstanced     => NodeInstance != null;

            public bool IsEditable      => _Parent.Parent.CanEditHierarchy;

            public bool IsCollectionElement => _Index >= 0;

            public Type DataType
            {
                get
                {
                    if (_Binding is Bindings.SingleDependencyBinding) return ((Bindings.SingleDependencyBinding)_Binding).DataType;
                    if (_Binding is Bindings.MultiDependencyBinding) return ((Bindings.MultiDependencyBinding)_Binding).DataType.GetElementType();
                    throw new NotSupportedException();
                }
            }            

            #endregion

            #region API

            private bool _EditableBarrier()
            {
                if (!IsEditable) _Dialogs.ShowErrorDialog("Hierarchy cannot be edited under the current configuration.\r\nSwitch to root configuration to edit.");
                return IsEditable;
            }

            private void _RemoveParameter()
            {
                if (!_EditableBarrier()) return;

                _SetDependencyId(Guid.Empty);
            }

            private void _SetNewDependencyNode()
            {
                if (!_EditableBarrier()) return;

                var plugins = _Parent.Parent.PipelineServices.GetPluginManager();

                var compatibleNodes = plugins
                    .PluginTypes
                    .OfType<Factory.ContentFilterTypeInfo>()
                    .Where(item => DataType.IsAssignableFrom(item.OutputType));

                var r = _Dialogs.ShowNewNodeDialog(null, compatibleNodes);
                if (r != null) _SetDependency(r);
            }

            

            private Guid _GetDependencyId()
            {
                if (_Binding is Bindings.SingleDependencyBinding) return ((Bindings.SingleDependencyBinding)_Binding).GetDependency();
                if (_Binding is Bindings.MultiDependencyBinding) return ((Bindings.MultiDependencyBinding)_Binding).GetDependency(_Index);
                throw new NotSupportedException();
            }


            private void _SetDependency(Factory.ContentBaseTypeInfo value)
            {
                if (!_EditableBarrier()) return;

                if (value == null) { _SetDependencyId(Guid.Empty); }

                

                if (value is Factory.ContentFilterTypeInfo)
                {
                    var nodeId = _Parent.Parent.AddNode(value);
                    _SetDependencyId(nodeId);

                    // create an instance to extract the bindings, so we can create the default nodes
                    // var ninst = ((Factory.ContentFilterTypeInfo)value).CreateInstance(this._Parent.Parent._Parent.GetBuildSettings());
                    // ninst.CreateBindings(null);
                    
                }
            }

            private void _SetDependencyId(Guid nodeId)
            {
                if (!_EditableBarrier()) return;

                if (_Binding is Bindings.SingleDependencyBinding) ((Bindings.SingleDependencyBinding)_Binding).SetDependency(nodeId);
                if (_Binding is Bindings.MultiDependencyBinding) ((Bindings.MultiDependencyBinding)_Binding).SetDependency(_Index,nodeId);

                _Parent.Parent.UpdateGraph();
            }
            
            private void _RemoveElement()
            {
                if (_Index < 0) return;
                if (!_EditableBarrier()) return;

                var mb = (Bindings.MultiDependencyBinding)_Binding;
                if (mb == null) return;

                // TODO: here it should call _SetDependencyId(empty) to remove the node, but without calling UpdateGraph

                mb.RemoveSlot(_Index);

                _Parent.Parent.UpdateGraph();
            }

            #endregion
        }

        public class ArrayDependencyView : BindableBase
        {
            #region lifecycle

            internal static ArrayDependencyView _Create(Node parent, Bindings.MultiDependencyBinding binding)
            {
                if (parent == null) return null;                

                return new ArrayDependencyView(parent, binding);
            }

            private ArrayDependencyView(Node parent, Bindings.MultiDependencyBinding binding)
            {
                _Parent = parent;
                _Binding = binding;                

                AddParameterSlotCmd = new RelayCommand(_AddSlot);
            }

            #endregion

            #region data

            private readonly Node _Parent;

            private readonly Bindings.MultiDependencyBinding _Binding;            

            #endregion

            #region commands

            public ICommand AddParameterSlotCmd { get; private set; }

            #endregion

            #region properties

            public string DisplayName => _Binding.DisplayName;            

            public SingleDependencyView[] Slots
            {
                get
                {
                    var nodeIds = _Binding.GetDependencies();

                    return Enumerable.Range(0, nodeIds.Length)
                        .Select(idx => SingleDependencyView._Create(_Parent, _Binding, idx))
                        .ToArray();
                }
            }

            public System.Windows.Controls.Orientation ItemsControlPanelOrientation { get { return _Binding.ArrangeItemsHorizontal ? System.Windows.Controls.Orientation.Horizontal : System.Windows.Controls.Orientation.Vertical; } }

            #endregion

            #region API

            private void _AddSlot() { _Binding.AddSlot(); _Parent.Parent.UpdateGraph(); }

            private void _RemoveSlot(int index) { _Binding.RemoveSlot(index); _Parent.Parent.UpdateGraph(); }

            #endregion
        }
        
    }
}
