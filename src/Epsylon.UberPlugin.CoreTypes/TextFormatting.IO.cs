﻿using System;
using System.Collections.Generic;

namespace Epsylon.UberPlugin.CoreTypes
{    
    using UberFactory;

    using TEXTFUNC = Func<String, String>;

    [SDK.Icon("✍")]
    [SDK.ContentNode(nameof(AssignText))]
    [SDK.Title("Text")]
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
    [SDK.Title("From File")]
    public sealed class TextReader : SDK.FileReader<String>
    {
        public override string GetFileFilter() { return "Text Files|*.txt"; }

        protected override string ReadFile(SDK.ImportContext stream)
        {
            return GetSharedSettings<TextFormattingSettings>()?.ReadText(stream);
        }
    }

    [SDK.Icon("💾")]
    [SDK.ContentNode(nameof(TextWriter))]
    [SDK.Title("Write Text to File")]
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
    }


    [SDK.ContentNode(nameof(TextBatchProcessor))]
    [SDK.Title("Process batch of Text Files")]
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

    [SDK.ContentNode(nameof(TextBatchMerger))]
    [SDK.Title("Merge batch of Text Files")]
    public sealed class TextBatchMerger : SDK.BatchMerge<String, String>
    {
        protected override IEnumerable<string> GetFileInExtensions() { yield return "txt"; }

        protected override string ReadFile(SDK.ImportContext stream)
        {
            return GetSharedSettings<TextFormattingSettings>()?.ReadText(stream);
        }

        protected override string Merge(string product, string value)
        {
            return product + value;
        }        
    }



    


    
}
