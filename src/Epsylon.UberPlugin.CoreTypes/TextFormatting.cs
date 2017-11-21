using System;

namespace Epsylon.UberPlugin.CoreTypes
{
    using System.Collections.Generic;
    using UberFactory;

    using TEXTFUNC = Func<String, String>;

    static class _TextExtensions
    {
        public static string SanitizeUserInput(this string value, bool escape = true)
        {
            if (value == null) return string.Empty;

            if (escape) value = System.Text.RegularExpressions.Regex.Unescape(value);

            return value;
        }

        public static string Process(this TEXTFUNC[] transforms, string value)
        {
            if (transforms == null) return value;

            foreach (var xform in transforms)
            {
                if (xform == null) continue;
                value = xform(value ?? String.Empty);
            }

            return value;
        }
    }

    [SDK.ContentNode(nameof(AssignText))]
    [SDK.ContentMetaData("Title", "Text")]
    public sealed class AssignText : SDK.ContentFilter<String>
    {
        [SDK.InputValue(nameof(Escape))]        
        [SDK.InputMetaData("Title", "Esc")]
        [SDK.InputMetaData("Default", true)]
        public bool Escape { get; set; }

        [SDK.InputValue(nameof(Value))]        
        [SDK.InputMetaData("Title", "Value")]
        [SDK.InputMetaData("MultiLine", true)]
        public String Value { get; set; }

        protected override String Evaluate()
        {
            return Value.SanitizeUserInput(Escape);
        }
    }


    [SDK.ContentNode(nameof(TextReader))]
    [SDK.ContentMetaData("Title", "From File")]
    public sealed class TextReader : SDK.FileReader<String>
    {
        public override string GetFileFilter() { return "Text Files|*.txt"; }

        protected override string ReadFile(SDK.ImportContext stream) { return stream.ReadAllText(); }
    }

    [SDK.ContentNode(nameof(TextWriter))]
    [SDK.ContentMetaData("Title", "Write Text to File")]
    public sealed class TextWriter : SDK.FileWriter
    {
        [SDK.InputNode(nameof(Value))]
        public String Value { get; set; }

        protected override string GetFileExtension()
        {
            return "txt";
        }

        protected override void WriteFile(SDK.ExportContext stream)
        {
            var settings = GetSharedSettings<TextFormattingSettings>();

            settings.WriteText(stream, Value);
        }
    }


    [SDK.ContentNode(nameof(TextBatchProcessor))]
    [SDK.ContentMetaData("Title", "Process batch of Text Files")]
    public sealed class TextBatchProcessor : SDK.BatchProcessor<String, String>
    {
        [SDK.InputNode("Transforms", true)]
        [SDK.InputMetaData("Panel", "VerticalList")]
        public TEXTFUNC[] Transforms { get; set; }

        protected override IEnumerable<string> GetFileInExtensions() { yield return "txt"; }

        protected override string GetFileOutExtension() { return "txt"; }

        protected override string ReadFile(SDK.ImportContext stream)
        {
            return stream.ReadAllText();
        }

        protected override string Transform(string value)
        {
            return Transforms.Process(value);
        }        

        protected override void WriteFile(SDK.ExportContext stream, string value)
        {
            stream.WriteAllText(value);
        }
    }

    [SDK.ContentNode(nameof(TextBatchMerger))]
    [SDK.ContentMetaData("Title", "Merge batch of Text Files")]
    public sealed class TextBatchMerger : SDK.BatchMerge<String, String>
    {
        protected override IEnumerable<string> GetFileInExtensions() { yield return "txt"; }

        protected override string ReadFile(SDK.ImportContext stream)
        {
            return stream.ReadAllText();
        }

        protected override string Merge(string product, string value)
        {
            return product + value;
        }        
    }



    [SDK.ContentNode("FormatText")]
    [SDK.ContentMetaData("Title", "Formatted Text")]
    [SDK.ContentMetaData("TitleFormat", "{0} Formatted")]
    public sealed class FormatText : SDK.ContentFilter<String>
    {
        [SDK.InputNode(nameof(Value))]
        public String Value { get; set; }

        [SDK.InputNode("Transforms", true)]
        [SDK.InputMetaData("Panel", "VerticalList")]
        public TEXTFUNC[] Transforms { get; set; }

        protected override string Evaluate()
        {
            return Transforms.Process(Value);
        }
    }


    
}
