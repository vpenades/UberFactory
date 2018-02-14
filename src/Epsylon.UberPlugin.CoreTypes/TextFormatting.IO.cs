using System;
using System.Collections.Generic;

namespace Epsylon.UberPlugin.CoreTypes
{    
    using UberFactory;

    using TEXTFUNC = Func<String, String>;

    [SDK.Icon(Constants.ICON_USERINPUT), SDK.Title("Text")]
    [SDK.ContentNode(nameof(AssignText))]    
    public sealed class AssignText : SDK.ContentFilter<String>
    {
        [SDK.Group(0)]
        [SDK.InputValue(nameof(Escape))]        
        [SDK.Title(TextFormatting.ESCAPETEXT_ICON)]
        [SDK.Default(true)]
        public bool Escape { get; set; }

        [SDK.Group(0)]
        [SDK.InputValue(nameof(Value))]        
        [SDK.Title("Value")]
        [SDK.MetaData("MultiLine", true)]
        public String Value { get; set; }

        protected override String Evaluate()
        {
            return Value.SanitizeUserInput(Escape);
        }
    }

    [SDK.ContentNode(nameof(TextReader))]    
    public sealed class TextReader : SDK.FileReader<String>
    {
        public override string GetFileFilter() { return "Text Files|*.txt"; }

        protected override string ReadFile(SDK.ImportContext stream)
        {
            return GetSharedSettings<TextFormattingSettings>()?.ReadText(stream);
        }        
    }

    [SDK.Icon(Constants.ICON_TEXT), SDK.Title("Write Text to File")]
    [SDK.ContentNode(nameof(TextWriter))]    
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
            GetSharedSettings<TextFormattingSettings>()?.WriteText(stream, Value);
        }

        protected override object EvaluatePreview(SDK.PreviewContext previewContext)
        {
            return Value;
        }
    }

    [SDK.Icon(Constants.ICON_TEXT), SDK.Title("Process batch of Text Files")]
    [SDK.ContentNode(nameof(TextBatchProcessor))]    
    public sealed class TextBatchProcessor : SDK.BatchProcessor<String, String>
    {
        [SDK.InputNode("Transforms", true)]
        [SDK.ItemsPanel("VerticalList")]
        public TEXTFUNC[] Transforms { get; set; }

        protected override IEnumerable<string> GetFileInExtensions() { yield return "txt"; }

        protected override string GetFileOutExtension() { return "txt"; }

        protected override string ReadFile(SDK.ImportContext stream)
        {
            return GetSharedSettings<TextFormattingSettings>()?.ReadText(stream);
        }

        protected override string Transform(string value)
        {
            return Transforms.Process(value);
        }        

        protected override void WriteFile(SDK.ExportContext stream, string value)
        {
            GetSharedSettings<TextFormattingSettings>()?.WriteText(stream,value);
        }
    }

    [SDK.Icon(Constants.ICON_FILEBATCH), SDK.Title("Merge batch of Text Files")]
    [SDK.ContentNode(nameof(TextBatchMerger))]    
    public sealed class TextBatchMerger : SDK.BatchMerge<String, String>
    {
        [SDK.InputValue("Separator")]        
        public String Separator { get; set; }

        protected override IEnumerable<string> GetFileInExtensions() { yield return "txt"; }

        protected override string ReadFile(SDK.ImportContext stream)
        {
            return GetSharedSettings<TextFormattingSettings>()?.ReadText(stream);
        }

        protected override string Merge(string product, string value)
        {
            if (String.IsNullOrEmpty(Separator)) return product + value;

            return product + Separator + value;
        }        
    }



    


    
}
