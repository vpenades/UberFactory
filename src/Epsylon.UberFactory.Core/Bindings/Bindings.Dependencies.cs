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

        public static MemberBinding Create(Description bindDesc, bool isMulti)
        {
            var propertyType = bindDesc.Member.GetAssignType();

            var isArray = propertyType.IsArray;            
            return isMulti && isArray ? (MemberBinding)new MultiDependencyBinding(bindDesc) : new SingleDependencyBinding(bindDesc);
        }

        public DependencyBinding(Description pvd) : base(pvd)
        {
            _Properties = pvd.Properties;
        }

        #endregion

        #region data

        protected IPropertyProvider _Properties;

        #endregion

        #region API

        public Boolean HasOwnValue => _Properties.GetValue(this.SerializationKey, null) != _Properties.GetDefaultValue(this.SerializationKey, null);

        public void ClearEvaluatedResult()
        {
            // actually, set default value of binding.DataType

            SetEvaluatedResult(null);
        }

        public void SetEvaluatedResult(Object value) { SetInstanceValue(value); }

        public abstract void EvaluateAndAssign(Func<Guid, Object> nodeEvaluator);

        protected void SetSingleDependency(Guid nodeId)
        {
            _Properties.SetReferenceIds(this.SerializationKey, nodeId);
        }

        protected Guid GetSingleDependency()
        {
            return _Properties.GetReferenceIds(this.SerializationKey, new string[0]).FirstOrDefault();
        }

        protected void SetMultiDependency(Guid[] nodeIds)
        {
            _Properties.SetReferenceIds(this.SerializationKey, nodeIds);
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

            #if DEBUG
            var _debugCount = ids.Count;
            #endif

            ids.Add(Guid.Empty);
            SetDependencies(ids.ToArray());

            System.Diagnostics.Debug.Assert(GetDependencies().Length == _debugCount + 1, "ERROR, new slot was not added correctly");            
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
}
