using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        public static ContentObject Create(Type type)
        {
            if (type == null) return null;
            var node = (ContentObject)System.Activator.CreateInstance(type);            

            return node;
        }

        public static void ConfigureNode(this ContentObject node, IBuildContext bsettings, Func<Type,ContentObject> settingsResolver)
        {
            if (node._BuildContext != null) throw new InvalidOperationException("already initialized");

            node._BuildContext = bsettings ?? throw new ArgumentNullException(nameof(bsettings));
            
            if (node is ContentFilter)
            {
                if ((node as ContentFilter)._SharedContentResolver != null) throw new InvalidOperationException("already initialized");

                (node as ContentFilter)._SharedContentResolver = settingsResolver ?? throw new ArgumentNullException(nameof(settingsResolver));
            }
        }


        public static Object DebugNode(ContentFilter node, IMonitorContext monitor, Func<Type, ContentObject> sf)
        {
            #if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Launch();
            #endif

            return EvaluateNode(node, monitor, sf);
        }


        public static Object EvaluateNode(ContentFilter node, IMonitorContext monitor, Func<Type,ContentObject> sf)
        {
            if (node == null) return null;
            if (monitor == null) throw new ArgumentNullException(nameof(monitor));

            return node._Evaluate(monitor, sf);
        }

        public static Object PreviewNode(ContentFilter node, IMonitorContext monitor, Func<Type, ContentObject> sf)
        {
            if (node == null) return null;
            if (monitor == null) throw new ArgumentNullException(nameof(monitor));

            return node._Preview(monitor, sf);
        }


    }
}
