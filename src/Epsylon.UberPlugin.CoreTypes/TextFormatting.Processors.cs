using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberPlugin.CoreTypes
{    
    using UberFactory;

    using TEXTFUNC = Func<String, String>;

    [SDK.ContentNode("FormatText")]
    [SDK.Title("Formatted Text")]
    [SDK.TitleFormat( "{0} Formatted")]
    public sealed class FormatText : SDK.ContentFilter<String>
    {
        [SDK.InputNode(nameof(Value))]
        public String Value { get; set; }

        [SDK.InputNode("Transforms", true)]
        [SDK.ItemsPanel("VerticalList")]
        public TEXTFUNC[] Transforms { get; set; }

        protected override string Evaluate()
        {
            return Transforms.Process(Value);
        }
    }

    public abstract class TextFunction : SDK.ContentFilter<TEXTFUNC>
    {        
        [SDK.InputValue(nameof(Boolean))]
        [SDK.Group(0), SDK.Title(TextFormatting.ENABLED_ICON)]
        [SDK.Default(true)]
        public Boolean Enabled { get; set; }

        protected override TEXTFUNC Evaluate()
        {
            if (!Enabled) return val => val; // pass through

            return Process;
        }

        protected abstract String Process(String value);
    }

    [SDK.ContentNode("AppendTextFunction")]
    [SDK.Title("Append")]
    public sealed class AppendTextFunction : TextFunction
    {
        [SDK.InputValue(nameof(Escape))]
        [SDK.Group(0), SDK.Title(TextFormatting.ESCAPETEXT_ICON)]
        [SDK.Default(true)]
        public bool Escape { get; set; }

        [SDK.InputValue(nameof(Text))]
        [SDK.Title("Text")]
        public String Text { get; set; }

        protected override String Process(String value)
        {
            var newText = Text.SanitizeUserInput(Escape);

            if (string.IsNullOrEmpty(value)) return newText;

            return value + newText;
        }
    }

    [SDK.ContentNode("PrependTextFunction")]
    [SDK.Title("Prepend")]
    public sealed class PrependTextFunction : TextFunction
    {
        [SDK.InputValue(nameof(Escape))]
        [SDK.Group(0), SDK.Title(TextFormatting.ESCAPETEXT_ICON)]        
        [SDK.Default(true)]
        public bool Escape { get; set; }

        [SDK.Group(0)]
        [SDK.InputValue(nameof(Text))]
        [SDK.Title("Text")]
        public String Text { get; set; }

        protected override String Process(String value)
        {
            var newText = Text.SanitizeUserInput(Escape);

            if (string.IsNullOrEmpty(value)) return newText;

            return newText + value;
        }
    }

    [SDK.ContentNode("ReplaceTextFunction")]
    [SDK.Title("Replace")]
    public sealed class ReplaceTextFunction : TextFunction
    {
        [SDK.InputValue(nameof(OldEscape))]
        [SDK.Group("Old"), SDK.Title("Esc")]
        [SDK.Default(true)]
        public bool OldEscape { get; set; }

        [SDK.InputValue(nameof(OldString))]
        [SDK.Group("Old"), SDK.Title("Text")]
        public String OldString { get; set; }

        [SDK.InputValue(nameof(NewEscape))]
        [SDK.Group("New"), SDK.Title("Esc")]
        [SDK.Default(true)]
        public bool NewEscape { get; set; }

        [SDK.InputValue(nameof(NewString))]
        [SDK.Group("New"), SDK.Title("Text")]
        public String NewString { get; set; }

        protected override String Process(String value)
        {
            return value?.Replace(OldString.SanitizeUserInput(OldEscape), NewString.SanitizeUserInput(NewEscape));
        }
    }

    [SDK.ContentNode("TrimTextFunction")]
    [SDK.Title("Trim")]
    public sealed class TrimTextFunction : TextFunction
    {
        [SDK.InputValue(nameof(TrimStart))]
        [SDK.Group(0), SDK.Title("Start")]
        [SDK.Default(true)]
        public Boolean TrimStart { get; set; }

        [SDK.InputValue(nameof(TrimEnd))]
        [SDK.Group(0), SDK.Title("End")]
        [SDK.Default(true)]
        public Boolean TrimEnd { get; set; }

        protected override String Process(String value)
        {
            if (value == null) return null;

            if (TrimStart) value = value.TrimStart();
            if (TrimEnd) value = value.TrimEnd();

            return value;
        }
    }


    [SDK.ContentNode(nameof(InsertTextAtFunction))]
    [SDK.Title("Insert At")]
    public sealed class InsertTextAtFunction : TextFunction
    {
        [SDK.InputValue(nameof(NewEscape))]
        [SDK.Group("Text to Insert"), SDK.Title(TextFormatting.ESCAPETEXT_ICON)]
        [SDK.Default(true)]
        public bool NewEscape { get; set; }

        [SDK.InputValue(nameof(NewString))]
        [SDK.Group("Text to Insert"), SDK.Title("Text")]
        public String NewString { get; set; }

        [SDK.InputValue(nameof(Offset))]
        [SDK.Title("From Start")]
        public int Offset { get; set; }

        protected override String Process(String value)
        {
            if (value == null) return null;

            while (Offset > value.Length) value += " ";

            value = value.Insert(Offset, NewString.SanitizeUserInput(NewEscape));

            return value;
        }
    }


    [SDK.ContentNode(nameof(ChangeTextCaseFunction))]
    [SDK.Title("Case")]
    public sealed class ChangeTextCaseFunction : TextFunction
    {
        [SDK.InputValue(nameof(CaseType))]
        [SDK.Title("Case")]
        [SDK.ViewStyle("ComboBox")]
        [SDK.Default("Upper")]
        [SDK.MetaData("Values", new string[] { "Upper", "Lower", "Title" })]
        public string CaseType { get; set; }

        protected override String Process(String value)
        {
            if (value == null) return null;

            var settings = GetSharedSettings<TextFormattingSettings>();

            if (CaseType == "Upper") value = settings.CurrentCulture.TextInfo.ToUpper(value);
            if (CaseType == "Lower") value = settings.CurrentCulture.TextInfo.ToLower(value);
            if (CaseType == "Title") value = settings.CurrentCulture.TextInfo.ToTitleCase(value);

            return value;
        }
    }

}
