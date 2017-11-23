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
        public abstract class MetaDataKeyAttribute : Attribute
        {
            public MetaDataKeyAttribute(string key) { Key = key; }
            public string Key { get; private set; }            
        }        

        /// <summary>
        /// defines extra information for a given input property
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
        public class MetaDataAttribute : MetaDataKeyAttribute
        {
            public MetaDataAttribute(string key, Object value) : base(key) { Value = value; }
            public Object Value { get; private set; }
        }

        /// <summary>
        /// defines extra information for a given input property
        /// </summary>
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
        public class MetaDataEvaluateAttribute : MetaDataKeyAttribute
        {
            public MetaDataEvaluateAttribute(string key, string propertyName) : base(key) { PropertyName = propertyName; }

            public MetaDataEvaluateAttribute(string key, Type st, string propertyName) : base(key) { SharedType = st; PropertyName = propertyName; }

            public Type SharedType { get; private set; }
            public string PropertyName { get; private set; }
        }        


        public class TitleAttribute : MetaDataAttribute
        {
            public TitleAttribute(string title) : base("Title", title) { }
        }

        [AttributeUsage(AttributeTargets.Class)]
        public class TitleFormatAttribute : MetaDataAttribute
        {
            public TitleFormatAttribute(string titleFormat) : base("Title", titleFormat) { }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class DefaultAttribute : MetaDataAttribute
        {
            public DefaultAttribute(Object value) : base("Default", value) { }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class MinimumAttribute : MetaDataAttribute
        {
            public MinimumAttribute(Object value) : base("Minimum", value) { }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class MaximumAttribute : MetaDataAttribute
        {
            public MaximumAttribute(Object value) : base("Maximum", value) { }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class ItemsPanelAttribute : MetaDataAttribute
        {
            public ItemsPanelAttribute(string panelType) : base("Panel", panelType) { }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class GroupAttribute : MetaDataAttribute
        {
            public GroupAttribute(string groupName) : base("Group", groupName) { }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class ViewStyleAttribute : MetaDataAttribute
        {
            public ViewStyleAttribute(string control) : base("ViewStyle", control) { }
        }



    }

}
