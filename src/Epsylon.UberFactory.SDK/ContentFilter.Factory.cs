using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        public static ContentFilter Create(Type type, IBuildContext bsettings)
        {
            if (type == null) return null;
            var processor = (ContentFilter)System.Activator.CreateInstance(type);

            processor._BuildContext = bsettings ?? throw new ArgumentNullException(nameof(bsettings));            

            return processor;
        }


        public static Object DebugNode(ContentFilter node, IMonitorContext monitor)
        {
            #if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Launch();
            #endif

            return EvaluateNode(node, monitor);
        }


        public static Object EvaluateNode(ContentFilter node, IMonitorContext monitor)
        {
            if (node == null) return null;
            if (monitor == null) throw new ArgumentNullException(nameof(monitor));

            return node._Evaluate(monitor);
        }

        public static Object PreviewNode(ContentFilter node, IMonitorContext monitor)
        {
            if (node == null) return null;
            if (monitor == null) throw new ArgumentNullException(nameof(monitor));

            return node._Preview(monitor);
        }


    }
}
