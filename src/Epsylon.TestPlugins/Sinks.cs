using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.TestPlugins
{
    using UberFactory;

    [SDK.ContentFilter(nameof(TextSink))]
    public sealed class TextSink : SDK.ContentFilter
    {
        [SDK.InputNode(nameof(Source))]
        public String Source { get; set; }

        protected override object EvaluateObject()
        {
            return null;
        }
    }

    [SDK.ContentFilter(nameof(MultiTextSink))]
    public sealed class MultiTextSink : SDK.ContentFilter
    {
        [SDK.InputNode(nameof(Source),true)]
        public String[] Source { get; set; }

        protected override object EvaluateObject()
        {
            return null;
        }
    }

    [SDK.ContentFilter(nameof(IntegerSink))]
    public sealed class IntegerSink : SDK.ContentFilter
    {
        [SDK.InputNode(nameof(Source))]
        public Int32 Source { get; set; }

        protected override object EvaluateObject()
        {
            return null;
        }
    }
}
