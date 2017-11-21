﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// make factory internals visible only to Core
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Epsylon.UberFactory.Core")]

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        internal static ContentObject Create(Type type)
        {
            if (type == null) return null;
            var node = (ContentObject)System.Activator.CreateInstance(type);            

            return node;
        }        

        internal static Object DebugNode(ContentFilter node, IMonitorContext monitor)
        {
            #if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Launch();
            #endif

            return EvaluateNode(node, monitor);
        }

        internal static Object EvaluateNode(ContentFilter node, IMonitorContext monitor)
        {
            if (node == null) return null;
            if (monitor == null) throw new ArgumentNullException(nameof(monitor));

            return node._EvaluateObject(monitor);
        }

        internal static Object PreviewNode(ContentFilter node, IMonitorContext monitor)
        {
            if (node == null) return null;
            if (monitor == null) throw new ArgumentNullException(nameof(monitor));

            return node._EvaluatePreview(monitor);
        }
    }
}
