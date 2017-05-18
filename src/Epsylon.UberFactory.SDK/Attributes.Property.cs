using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    partial class SDK
    {
        #region input metadata modifiers       

        /// <summary>
        /// defines extra information for a given input property
        /// </summary>
        [AttributeUsageAttribute(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
        public class InputMetaDataAttribute : MetaDataKeyAttribute
        {
            public InputMetaDataAttribute(string key,Object value) :base(key) { Value = value; }
            public Object Value { get; private set; }
        }

        /// <summary>
        /// defines extra information for a given input property
        /// </summary>
        [AttributeUsageAttribute(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
        public class InputMetaDataEvaluateAttribute : MetaDataKeyAttribute
        {
            public InputMetaDataEvaluateAttribute(string key, string propertyName) : base(key) { PropertyName = propertyName; }
            public string PropertyName { get; private set; }
        }

        #endregion

        #region input properties

        [AttributeUsageAttribute(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public abstract class InputPropertyAttribute : Attribute
        {
            #region lifecycle
            
            public InputPropertyAttribute(string serializationKey)
            {
                if (string.IsNullOrWhiteSpace(serializationKey)) throw new ArgumentNullException(nameof(serializationKey));
                if (serializationKey.ToCharArray().Any(c => char.IsWhiteSpace(c))) throw new ArgumentException("Serialization key cannot contain white spaces", nameof(serializationKey));

                this.SerializationKey = serializationKey;                
            }

            #endregion

            #region data

            public virtual bool IsDependency { get { return false; } }

            public string SerializationKey { get; private set; }

            #endregion
        }


        /// <summary>
        /// Declares a property of a Component as an Input Value, which can be edited and serialized
        /// </summary>
        public sealed class InputValueAttribute : InputPropertyAttribute
        {
            public InputValueAttribute(string serializationKey) : base(serializationKey) { }
        }

        /// <summary>
        /// Declares a property of a Component as an Input Component, which can be edited and serialized
        /// </summary>
        public sealed class InputNodeAttribute : InputPropertyAttribute
        {
            #region lifecycle

            /// <summary>
            /// 
            /// </summary>
            /// <param name="serializationKey">key uaws for serialization</param>            
            /// <param name="isCollection"></param>
            public InputNodeAttribute(string serializationKey,  bool isCollection=false) : base(serializationKey) { this.IsCollection = isCollection; }

            public override bool IsDependency { get { return true; } }

            public bool IsCollection { get; private set; }

            #endregion                    
        }

        /// <summary>
        /// Declares a property of a Component as an Input Pipeline, which can be edited and serialized
        /// </summary>
        public sealed class InputPipelineAttribute : InputPropertyAttribute
        {
            // must define here inputs and output so we can restrict the pipeline choices.

            #region lifecycle

            public InputPipelineAttribute(string serializationKey) : base(serializationKey) { }

            public override bool IsDependency { get { return true; } }

            #endregion                    
        }        

        #endregion
    }



}
