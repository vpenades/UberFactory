using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    partial class SDK
    {
        

        [AttributeUsageAttribute(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
        public class ContentNodeAttribute : Attribute
        {
            #region lifecycle           

            /// <summary>
            /// Declares the current class as a Tail Component with no return type
            /// </summary>
            /// <param name="serializationKey">text displayed in the UI</param>
            public ContentNodeAttribute(string serializationKey)
            {
                this.SerializationKey = serializationKey;                
            }

            #endregion

            #region properties

            /// <summary>
            /// key to use to serialize this node
            /// </summary>
            public string SerializationKey { get; private set; }             

            #endregion           
        }

        [AttributeUsageAttribute(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
        public class ContentMetaDataAttribute : MetaDataKeyAttribute
        {
            public ContentMetaDataAttribute(string key, Object value) : base(key) {Value = value; }            
            public Object Value { get; private set; }           
        }
    }
}
