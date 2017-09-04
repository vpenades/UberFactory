using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.TestPlugins
{
    using UberFactory;

    [SDK.ContentNode(nameof(TextSink))]
    public sealed class TextSink : SDK.ContentFilter
    {
        [SDK.InputNode(nameof(Source))]
        public String Source { get; set; }

        protected override object EvaluateObject()
        {
            return null;
        }
    }

    [SDK.ContentNode(nameof(MultiTextSink))]
    public sealed class MultiTextSink : SDK.ContentFilter
    {
        [SDK.InputNode(nameof(Source),true)]
        public String[] Source { get; set; }

        protected override object EvaluateObject()
        {
            return null;
        }
    }

    [SDK.ContentNode(nameof(IntegerSink))]
    public sealed class IntegerSink : SDK.ContentFilter
    {
        [SDK.InputNode(nameof(Source))]
        public Int32 Source { get; set; }

        protected override object EvaluateObject()
        {
            return null;
        }
    }

    [SDK.ContentNode(nameof(IntegerWriter))]
    public sealed class IntegerWriter : SDK.FileWriter
    {
        [SDK.InputNode(nameof(Source))]
        public Int32 Source { get; set; }

        protected override string GetFileExtension()
        {
            return "Text File|*.txt";
        }

        protected override void WriteFile(SDK.ExportContext stream)
        {
            stream.WriteAllText(Source.ToString());
        }
    }
}
