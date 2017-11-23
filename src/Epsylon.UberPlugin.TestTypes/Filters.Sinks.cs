using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.TestPlugins
{
    using UberFactory;

    [SDK.ContentNode(nameof(TextSink))]
    [SDK.Title("Text Sink")]
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
    [SDK.Title("Text Array Sink")]
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
    [SDK.Title("Integer Sink")]
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
    [SDK.Title("Write Integer to File")]
    public sealed class IntegerWriter : SDK.FileWriter
    {
        [SDK.InputNode(nameof(Source))]
        public Int32 Source { get; set; }

        protected override string GetFileExtension()
        {
            return "txt";
        }

        protected override void WriteFile(SDK.ExportContext stream)
        {
            stream.WriteAllText(Source.ToString());
        }
    }
}
