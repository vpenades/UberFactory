﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.TestPlugins
{
    using UberFactory;
    
    [SDK.ContentNode("MainSettings1")]
    [SDK.ContentMetaData("Title","Main Settings")]
    public class MainSettings1 : SDK.ContentObject
    {
        [SDK.InputValue(nameof(Value1))]
        public int Value1 { get; set; }

        [SDK.InputValue(nameof(Value2))]
        public int Value2 { get; set; }
    }
}