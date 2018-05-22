using System;
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
        /// <summary>
        /// Implemented by <see cref="Pipeline"/> and <see cref="SingleDependencyView"/> to use common and consistent child instance management
        /// </summary>        
        public interface IChildEditCommands
        {
            /// <summary>
            /// Name of the child, when it is instanced
            /// </summary>
            String DisplayName { get; }            

            /// <summary>
            /// Command to remove the child
            /// </summary>
            ICommand ClearCurrentCmd { get; }            

            /// <summary>
            /// Command to set an empty child value  for the current configuration, overriding any parent value.
            /// </summary>
            ICommand SetCurrentEmptyCmd { get; }            

            /// <summary>
            /// command to set a selected child for the current configuration, overriding any parent value.
            /// </summary>
            ICommand SetCurrentValueCmd { get; }            

            ICommand ViewCurrentPreviewCmd { get; }            
        }

        public interface INodeViewServices
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

            Boolean IsChildConfiguration { get; }

            void SetAsCurrentResultView(Guid id); // EvaluatePreview();

            Guid AddNode(Factory.ContentBaseInfo cbinfo);

            void UpdateGraph();

            #endregion
        }

        public interface IPipelineViewServices
        {
            Evaluation.BuildContext GetBuildSettings();
            Factory.Collection GetPluginManager();            
            ProjectDOM.Settings GetSharedSettings(Type t);

            Type GetRootOutputType();
        }




        public class Pipeline : BindableBase , INodeViewServices , IChildEditCommands
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

                ClearCurrentCmd       = new RelayCommand(_ClearCurrentRootNode, arg => this.IsInstanced && !this.IsChildConfiguration);
                SetCurrentEmptyCmd    = new RelayCommand(()=> {}, arg => false); // this command is disabled because it only makes sense for child configurations, and root's don'g have per-cfg values
                SetCurrentValueCmd    = new RelayCommand(_SetCurrentRootNode, arg => this.IsEmpty && !this.IsChildConfiguration);
                ViewCurrentPreviewCmd = new RelayCommand(() => { if (Content is Node node) node.SetAsCurrentResultView(); } , arg => IsInstanced );
            }

            #endregion

            #region commands

            public ICommand ClearCurrentCmd { get; private set; }

            public ICommand SetCurrentEmptyCmd { get; private set; }

            public ICommand SetCurrentValueCmd { get; private set; }

            public ICommand ViewCurrentPreviewCmd { get; private set; }

            #endregion

            #region data

            internal readonly IPipelineViewServices _Parent;
            internal readonly ProjectDOM.Pipeline _PipelineDom;

            internal Evaluation.PipelineInstance _PipelineInstance;            

            private Exception _Exception;

            #endregion

            #region properties            

            public Node[] Nodes             => _PipelineDom.Nodes.Select(f => Node.Create(this, f.Identifier)).ToArray();

            public bool IsEmpty             => _PipelineDom.RootIdentifier == Guid.Empty || _PipelineDom.RootIdentifier == ProjectDOM.RESETTODEFAULT;

            public bool IsInstanced         => !IsEmpty;

            public String DisplayName       => Content?.DisplayName;

            public Node Content             => Node.Create(this, _PipelineDom.RootIdentifier);

            public Exception FailedState    => _Exception;

            public Boolean CanEditHierarchy => true;            

            public IPipelineViewServices PipelineServices => _Parent;

            public Boolean IsChildConfiguration => _Parent.GetBuildSettings().Configuration.Length > 1;

            // true if a specific value exists for the current configuration
            public bool IsOwnValue => _PipelineDom.RootIdentifier != Guid.Empty;

            #endregion

            #region API

            /// <summary>
            /// this method needs to be called whenever we change something in the DOM
            /// </summary>
            public void UpdateGraph()
            {
                // note: if the general document configuration changes, we must call setup again              
                
                try
                {
                    _Exception = null;
                    _PipelineInstance = Evaluation.PipelineInstance.CreatePipelineInstance(_PipelineDom, _Parent.GetPluginManager().CreateInstance, _Parent.GetSharedSettings);
                    _PipelineInstance.Setup(_Parent.GetBuildSettings());

                    _Exception = _PipelineInstance.FailedState;

                }
                catch (Exception ex)
                {
                    _Exception = ex;
                    _PipelineInstance = null;                    
                }

                RaiseChanged();                
            }            

            private void _ClearCurrentRootNode()
            {
                _PipelineDom.ClearNodes();
                UpdateGraph();
            }

            private void _SetCurrentRootNode()
            {
                // show Component Dialog 

                var compatibleNodes = _Parent
                    .GetPluginManager()
                    .PluginTypes
                    .OfType<Factory.ContentFilterInfo>()
                    .Where(item => item.OutputType == _Parent.GetRootOutputType())
                    .ToArray();                          

                var r = _Dialogs.ShowNewNodeDialog(null, compatibleNodes);
                if (r == null) return;

                // apply selected value

                _SetCurrentRootNode(r);
            }

            private void _SetCurrentRootNode(Factory.ContentFilterInfo t)
            {
                _PipelineDom.ClearNodes();
                _PipelineDom.RootIdentifier = _PipelineDom.AddNode(t);
                UpdateGraph();
            }

            public void SetAsCurrentResultView(Guid nodeId)
            {
                if (_PipelineInstance == null) return;

                Evaluation.IMultiFileContent result = null;

                using (var evaluator = _PipelineInstance.CreateEvaluator())
                {
                    result = evaluator.PreviewNode(nodeId).PreviewResult;
                }                    

                PreviewManager.ShowPreview(result);
            }

            public ProjectDOM.Node GetNodeDOM(Guid id) { return _PipelineDom.GetNode(id); }

            public string GetNodeDisplayName(Guid id)
            {
                return _PipelineInstance?.GetNodeInstance(id)?
                    .GetContentInfo()?
                    .DisplayName;
            }

            public string GetNodeDisplayFormat(Guid id)
            {
                return _PipelineInstance?
                    .GetNodeInstance(id)?
                    .GetContentInfo()?
                    .DisplayFormatName;
            }

            public IEnumerable<MemberBinding> CreateNodeBindings(Guid id)
            {
                if (_PipelineInstance == null) return Enumerable.Empty<MemberBinding>();
                return _PipelineInstance.CreateValueBindings(id);
            }

            public Guid AddNode(Factory.ContentBaseInfo cbinfo)
            {
                return _PipelineDom.AddNode(cbinfo);
            }

            #endregion
        }        

        public class Node : BindableBase
        {
            #region lifecycle

            public static bool IsEmptyGuid(Guid nodeId)
            {
                return nodeId == Guid.Empty || nodeId == ProjectDOM.RESETTODEFAULT;
            }

            public static Node Create(INodeViewServices p, Guid nodeId)
            {
                if (p == null) return null;                
                if (IsEmptyGuid(nodeId)) return null;
                if (p.GetNodeDOM(nodeId) == null) return null;                

                var node = new Node(p, nodeId);                

                var propertyBindings = p.CreateNodeBindings(nodeId);

                // now we need to wrap some bindings with UI friendly wrappers
                var propertyViews = propertyBindings.Cast<BINDING>().ToArray();
                for (int i = 0; i < propertyViews.Length; ++i)
                {
                    var bv = propertyViews[i];

                    if (bv is SingleDependencyBinding singleBinding) propertyViews[i] = SingleDependencyView._Create(node, singleBinding);
                    if (bv is MultiDependencyBinding multiBinding)   propertyViews[i] = ArrayDependencyView._Create(node, multiBinding);
                    
                }

                node._BindingsViews = propertyViews;

                return node;                
            }

            private Node(INodeViewServices p, Guid nodeId)
            {
                System.Diagnostics.Debug.Assert(p.GetNodeDOM(nodeId) != null);

                _Parent = p;
                _NodeId = nodeId;                
            }

            #endregion

            #region data

            private readonly INodeViewServices _Parent;
            private readonly Guid _NodeId;
            private BINDING[] _BindingsViews;

            #endregion

            #region properties

            public Guid Id                          => _NodeId;

            public INodeViewServices Parent          => _Parent;

            public ProjectDOM.Node NodeDescription  => _Parent.GetNodeDOM(_NodeId);

            // public SDK.ContentFilter NodeInstance   => _Parent._Evaluator.GetNodeInstance(_NodeId);            

            public string DisplayName               => _Parent.GetNodeDisplayName(_NodeId);

            public string PropertyFormatName        => _Parent.GetNodeDisplayFormat(_NodeId);

            public IEnumerable<BINDING> Bindings    => _BindingsViews;

            public IEnumerable<BINDING> BindingsGrouped => GroupedBindingsView.Group(_BindingsViews);            

            #endregion

            #region API            

            public void SetAsCurrentResultView() { _Parent.SetAsCurrentResultView(_NodeId); }
            
            #endregion
        }        

        /// <summary>
        /// Groups multiple bindable objects in an horizontal arrangement
        /// </summary>
        public class GroupedBindingsView : BindableBase
        {
            #region lifecycle

            public static IEnumerable<BINDING> Group(IEnumerable<BINDING> bindings)
            {
                var r = new List<BINDING>();

                foreach(var b in bindings)
                {
                    if (!(b is Bindings.ValueBinding vb)) { r.Add(b); continue; }

                    var gn = vb.GroupName; if (string.IsNullOrWhiteSpace(gn)) { r.Add(b); continue; }

                    var g = r.OfType<GroupedBindingsView>().FirstOrDefault(item => item._DisplayName == gn);
                    if (g == null) { g = new GroupedBindingsView(gn); r.Add(g); }

                    g._BindingsViews.Add(b);                    
                }

                return r;
            }

            private GroupedBindingsView(string displayName) { _DisplayName = displayName; }

            #endregion

            #region data

            private readonly string _DisplayName;

            private readonly List<BINDING> _BindingsViews = new List<BINDING>();

            #endregion

            #region properties

            public string DisplayName               => _DisplayName;

            public IReadOnlyList<BINDING> Bindings  => _BindingsViews.ToArray();

            #endregion
        }        

        public class SingleDependencyView : BindableBase , IChildEditCommands
        {
            // handles the binding of a single node dependency;
            // this view class has a dual mode function:
            // if _Binding is a SingleDependencyBinding, it defines the binding of a single dependency with its node.
            // if _Binding is a MultiDependencyBinding, it defines the binding of a single, indexed, dependency with an item of the array of _MultiDependencyBinding.

            #region lifecycle            

            internal static SingleDependencyView _Create(Node parent, SingleDependencyBinding binding)
            {
                if (parent == null) return null;
                if (binding == null) return null;                

                return new SingleDependencyView(parent, binding, -1);
            }

            internal static SingleDependencyView _Create(Node parent, MultiDependencyBinding binding, int index)
            {                
                if (parent == null) return null;
                if (binding == null) return null;
                if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));                

                return new SingleDependencyView(parent, binding, index);
            }

            private SingleDependencyView(Node parent, DependencyBinding binding, int index)
            {
                _Parent = parent;
                _Binding = binding;
                _Index = index;

                ViewCurrentPreviewCmd = new RelayCommand(() => { var view = NodeInstance; if (view != null) view.SetAsCurrentResultView(); }, arg => IsInstanced);

                if (_Index < 0)
                {
                    SetCurrentValueCmd = new RelayCommand(_SetNewDependencyNode, arg => IsEmpty);
                    ClearCurrentCmd = new RelayCommand(_RemoveParameter, arg => IsInstanced);
                    SetCurrentEmptyCmd = new RelayCommand(_SetEmptyOverrideValue, arg => IsChildConfiguration && IsOwnValue);
                    
                }
                else
                {
                    SetCurrentValueCmd = new RelayCommand(_SetNewDependencyNode, arg => IsEmpty);
                    ClearCurrentCmd = new RelayCommand(_RemoveParameter, arg => true);
                    SetCurrentEmptyCmd = new RelayCommand(_SetEmptyOverrideValue, arg => false);                    
                }
            }

            #endregion

            #region commands

            public ICommand SetCurrentValueCmd { get; private set; }

            public ICommand ClearCurrentCmd { get; private set; }

            public ICommand SetCurrentEmptyCmd { get; private set; }

            public ICommand ViewCurrentPreviewCmd { get; private set; }            

            #endregion

            #region data

            private readonly Node _Parent;

            private readonly DependencyBinding _Binding;
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

            public Node NodeInstance         => Node.Create(_Parent.Parent, _GetDependencyId());

            public bool IsEmpty              => Node.IsEmptyGuid(_GetDependencyId());

            public bool IsInstanced          => !IsEmpty;

            public bool IsEditable           => _Parent.Parent.CanEditHierarchy;

            public bool IsChildConfiguration => _Parent.Parent.IsChildConfiguration;

            // true if a specific value exists for the current configuration
            public bool IsOwnValue           => _Binding.HasOwnValue;            

            public bool IsCollectionElement  => _Index >= 0;

            public Type DataType
            {
                get
                {
                    if (_Binding is SingleDependencyBinding singleBinding) return singleBinding.DataType;
                    if (_Binding is MultiDependencyBinding multiBinding) return multiBinding.DataType.GetElementType();
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

                if (_GetDependencyId() != Guid.Empty) { _SetDependencyId(Guid.Empty); return; }

                if (_Index >= 0) _RemoveElementSlot();
                
            }

            private void _SetEmptyOverrideValue()
            {
                if (!_EditableBarrier()) return;

                _SetDependencyId(ProjectDOM.RESETTODEFAULT);
            }

            private void _SetNewDependencyNode()
            {
                if (!_EditableBarrier()) return;

                var plugins = _Parent.Parent.PipelineServices.GetPluginManager();

                var compatibleNodes = plugins
                    .PluginTypes
                    .OfType<Factory.ContentFilterInfo>()
                    .Where(item => DataType.IsAssignableFrom(item.OutputType));

                var r = _Dialogs.ShowNewNodeDialog(null, compatibleNodes);
                if (r != null) _SetDependency(r);
            }            

            private Guid _GetDependencyId()
            {
                if (_Binding is SingleDependencyBinding singleBinding) return singleBinding.GetDependency();
                if (_Binding is MultiDependencyBinding multiBinding) return multiBinding.GetDependency(_Index);
                throw new NotSupportedException();
            }

            private void _SetDependency(Factory.ContentBaseInfo value)
            {
                if (!_EditableBarrier()) return;

                if (value == null) { _SetDependencyId(Guid.Empty); }                

                if (value is Factory.ContentFilterInfo)
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

                if (_Binding is SingleDependencyBinding singleBinding) singleBinding.SetDependency(nodeId);
                if (_Binding is MultiDependencyBinding multiBinding) multiBinding.SetDependency(_Index,nodeId);

                _Parent.Parent.UpdateGraph();
            }
            
            private void _RemoveElementSlot()
            {
                if (!_EditableBarrier()) return;
                if (_Index < 0) return;
                if (_Binding is MultiDependencyBinding multiBinding)
                {
                    // TODO: here it should call _SetDependencyId(empty) to remove the node, but without calling UpdateGraph

                    multiBinding.RemoveSlot(_Index);
                    _Parent.Parent.UpdateGraph();
                }
            }

            #endregion
        }

        public class ArrayDependencyView : BindableBase
        {
            #region lifecycle

            internal static ArrayDependencyView _Create(Node parent, MultiDependencyBinding binding)
            {
                if (parent == null) return null;                

                return new ArrayDependencyView(parent, binding);
            }

            private ArrayDependencyView(Node parent, MultiDependencyBinding binding)
            {
                _Parent = parent;
                _Binding = binding;                

                AddParameterSlotCmd = new RelayCommand(_AddSlot);
            }

            #endregion

            #region data

            private readonly Node _Parent;

            private readonly MultiDependencyBinding _Binding;            

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
