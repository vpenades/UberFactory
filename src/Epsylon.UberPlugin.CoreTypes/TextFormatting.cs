using System;

namespace Epsylon.UberPlugin.CoreTypes
{
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
