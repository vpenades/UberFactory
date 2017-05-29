using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Bindings
{
    public abstract class DependencyBinding : MemberBinding
    {
        #region lifecycle

        public DependencyBinding(Description pvd) : base(pvd)
        {
            _Properties = pvd.Properties;
        }

        #endregion

        #region data

        protected IPropertyProvider _Properties;

        #endregion

        #region API

        public void ClearEvaluatedResult()
        {
            // actually, set default value of binding.DataType

            SetEvaluatedResult(null);
        }

        public void SetEvaluatedResult(Object value) { SetInstanceValue(value); }

        public abstract void EvaluateAndAssign(Func<Guid, Object> nodeEvaluator);

        protected void SetSingleDependency(Guid nodeId)
        {
            _Properties.SetNodeIds(this.SerializationKey, nodeId);
        }

        protected Guid GetSingleDependency()
        {
            return _Properties.GetReferenceIds(this.SerializationKey, new string[0]).FirstOrDefault();
        }

        protected void SetMultiDependency(Guid[] nodeIds)
        {
            _Properties.SetNodeIds(this.SerializationKey, nodeIds);
        }

        protected Guid[] GetMultiDependency()
        {
            return _Properties.GetReferenceIds(this.SerializationKey, new string[0]);
        }
        

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Single Dependency {SerializationKey}")]
    public sealed class SingleDependencyBinding : DependencyBinding
    {
        #region lifecycle

        public SingleDependencyBinding(Description pvd) : base(pvd) { }

        #endregion        

        #region API

        public Guid GetDependency() { return GetSingleDependency(); }

        public void SetDependency(ProjectDOM.Node node) { SetDependency(node.Identifier); }

        public void SetDependency(Guid nodeId) { SetSingleDependency(nodeId); }

        public override void EvaluateAndAssign(Func<Guid, Object> nodeEvaluator)
        {
            var dependencyNodeId = GetDependency();
            var r = nodeEvaluator(dependencyNodeId);
            SetEvaluatedResult(r);
        }

        #endregion        
    }

    [System.Diagnostics.DebuggerDisplay("Multi Dependency {SerializationKey}")]
    public sealed class MultiDependencyBinding : DependencyBinding
    {
        #region lifecycle

        public MultiDependencyBinding(Description pvd) : base(pvd) { }

        #endregion

        #region properties

        public bool ArrangeItemsHorizontal
        {
            get
            {
                if (GetMetaDataValue<string>("Panel", "HorizontalList") == "VerticalList") return false;

                return true;
            }
        }

        #endregion

        #region API

        public Guid[] GetDependencies() { return GetMultiDependency(); }

        public void SetDependencies(Guid[] nodeIds) { SetMultiDependency(nodeIds); }

        public Guid GetDependency(int index)
        {
            var ids = GetDependencies();
            return index < 0 || index >= ids.Length ? Guid.Empty : ids[index];
        }

        public void SetDependency(int index, Guid nodeId)
        {
            var ids = GetDependencies().ToList();
            while (ids.Count <= index) ids.Add(Guid.Empty);
            ids[index] = nodeId;
            SetDependencies(ids.ToArray());
        }

        public void AddSlot()
        {
            var ids = GetDependencies().ToList();
            ids.Add(Guid.Empty);
            SetDependencies(ids.ToArray());
        }

        public void RemoveSlot(int index)
        {
            var ids = GetDependencies().ToList();
            ids.RemoveAt(index);
            SetDependencies(ids.ToArray());
        }

        public override void EvaluateAndAssign(Func<Guid, Object> nodeEvaluator)
        {
            var dependencyNodeIds = GetDependencies();
            var rrr = dependencyNodeIds.Select(item => nodeEvaluator(item)).ToArray();
            SetEvaluatedResult(rrr);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Pipeline Dependency {SerializationKey}")]
    public sealed class PipelineDependencyBinding : DependencyBinding
    {
        #region lifecycle

        public PipelineDependencyBinding(Description pvd) : base(pvd) { }

        #endregion        

        #region API

        public void SetDependency(ProjectDOM.Template template) { SetSingleDependency(template.Identifier); }

        public void SetDependency(Guid templateId) { SetSingleDependency( templateId); }

        public Guid GetDependency() { return GetSingleDependency(); }

        public override void EvaluateAndAssign(Func<Guid, Object> nodeEvaluator)
        {
            var templateId = GetDependency();
            var r = nodeEvaluator(templateId);

            System.Diagnostics.Debug.Assert(r == null || r is SDK.IPipelineInstance, "Unexpected type");

            SetEvaluatedResult(r);
        }

        public Type[] GetTemplateSignature()
        {
            var desc = GetInputDesc<SDK.InputPipelineAttribute>();
            if (desc == null) return new Type[0];

            return new Type[] { desc.ReturnType }
            .Concat(desc.ArgumentTypes)
            .ToArray();
        }

        #endregion        
    }


}
