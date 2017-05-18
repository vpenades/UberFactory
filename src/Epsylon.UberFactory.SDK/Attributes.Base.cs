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
    }

}
