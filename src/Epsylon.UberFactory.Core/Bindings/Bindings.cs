using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Input;
using System.ComponentModel;

namespace Epsylon.UberFactory.Bindings
{
    

    /// <summary>
    /// Base class for all the bindings
    /// </summary>
    /// <remarks>
    /// Bindings are used to connect three different systems:
    /// - The DOM objects (via IDependencyProvider and IPropertyProvider) so the values edited can be stored and retrieved.
    /// - The current node instance, where the values are applied so we can execute the tasks
    /// - The WPF view using WPF bindings.
    /// </remarks>
    public abstract class MemberBinding : INotifyPropertyChanged
    {
        #region lifecycle

        public struct Description
        {            
            public IPropertyProvider Properties;    // properties container from where to read/write values
            public SDK.ContentObject Target;        // target instance object (write only)
            public MemberInfo Member;               // member of the target to write to
        }

        protected MemberBinding(Description pvd)
        {
            _MemberInfo = pvd.Member;            
            _TargetInstance = pvd.Target;
        }

        #endregion

        #region data

        /// <summary>
        /// Target instance object
        /// </summary>
        private readonly SDK.ContentObject _TargetInstance;

        /// <summary>
        /// Target property of _TargetInstance to write to
        /// </summary>
        protected readonly MemberInfo _MemberInfo;

        #endregion

        #region properties        

        public String               SerializationKey    => _MemberInfo.GetInputDescAttribute().SerializationKey;
        public Type                 DataType            => _MemberInfo.GetAssignType();
        public SDK.ContentObject    DataContext         => _TargetInstance;

        #endregion

        #region properties - editing

        public String DisplayName => GetMetaDataValue<String>("Title",_MemberInfo.Name);
        public String ToolTip     => GetMetaDataValue<String>("ToolTip",null);
        public String GroupName   => GetMetaDataValue<String>("Group", null);

        #endregion

        #region API        

        protected virtual void SetInstanceValue(Object value) { DataContext.TryAssign(_MemberInfo, value); }

        protected T GetInputDesc<T>() where T : SDK.InputPropertyAttribute { return _MemberInfo.GetInputDescAttribute() as T; }

        protected T GetMetaDataValue<T>(string key, T defval)
        {
            var attrib = _MemberInfo.GetCustomAttributes(true)
                .OfType<SDK.MetaDataKeyAttribute>()
                .FirstOrDefault(item => item.Key == key);

            if (attrib == null) return defval;

            return attrib.GetValue<T>(_TargetInstance, defval);
        }        

        #endregion

        #region INotifyPropertyChanged Members        

        protected virtual void RaiseChanged(params string[] ps)
        {
            if (PropertyChanged == null) return;

            if (ps == null || ps.Length == 0) { PropertyChanged(this, new PropertyChangedEventArgs(null)); return; }

            foreach (var p in ps) PropertyChanged(this, new PropertyChangedEventArgs(p));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        
    }

    /// <summary>
    /// A binding to an invalid property
    /// </summary>
    /// <remarks>
    /// When the factory cannot create a binding for a given property for whatever reason,
    /// it falls back to creating this binding.
    /// On screen it is displayed as an error binding to help developers identify the property.
    /// </remarks>
    public sealed class InvalidBinding : MemberBinding
    {
        #region lifecycle

        public InvalidBinding(Description pvd) : base(pvd) { }

        #endregion
    }



    
    


    






    


    



    


}