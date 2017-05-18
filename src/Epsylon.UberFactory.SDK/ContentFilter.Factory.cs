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
            if (bsettings == null) throw new ArgumentNullException(nameof(bsettings));

            var processor = (ContentFilter)System.Activator.CreateInstance(type);

            processor._BuildContext = bsettings;            

            return processor;
        }


        public static Object DebugNode(ContentFilter node, System.Threading.CancellationToken cancelToken, IProgress<float> progress)
        {
            #if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Launch();
            #endif

            return EvaluateNode(node, cancelToken, progress);
        }


        public static Object EvaluateNode(ContentFilter node, System.Threading.CancellationToken cancelToken, IProgress<float> progress)
        {
            if (node == null) return null;
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            return node._Evaluate(cancelToken, progress);
        }

        public static Object PreviewNode(ContentFilter node, System.Threading.CancellationToken cancelToken, IProgress<float> progress)
        {
            if (node == null) return null;
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            return node._Preview(cancelToken, progress);
        }


    }
}
