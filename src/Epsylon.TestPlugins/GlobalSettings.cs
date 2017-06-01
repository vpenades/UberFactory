using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.TestPlugins
{
    using UberFactory;
    
    [SDK.GlobalSettings("MainSettings1")]
    public class MainSettings1 : SDK.ContentObject
    {
        [SDK.InputValue(nameof(Value1))]
        public int Value1 { get; set; }
    }
}
