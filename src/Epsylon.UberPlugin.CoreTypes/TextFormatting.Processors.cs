using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberPlugin.CoreTypes
{    
    using UberFactory;

    using TEXTFUNC = Func<String, String>;

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

    public abstract class TextFunction : SDK.ContentFilter<TEXTFUNC>
    {
        

        [SDK.InputValue(nameof(Boolean))]
        [SDK.InputMetaData("Group", TextFormatting.INVISIBLE_GROUP), SDK.InputMetaData("Title", TextFormatting.ENABLED_ICON)]
        [SDK.InputMetaData("Default", true)]
        public Boolean Enabled { get; set; }

        protected override TEXTFUNC Evaluate()
        {
            if (!Enabled) return val => val; // pass through

            return Process;
        }

        protected abstract String Process(String value);
    }

    [SDK.ContentNode("AppendTextFunction")]
    [SDK.ContentMetaData("Title", "Append")]
    public sealed class AppendTextFunction : TextFunction
    {
        [SDK.InputValue(nameof(Escape))]
        [SDK.InputMetaData("Group", TextFormatting.INVISIBLE_GROUP), SDK.InputMetaData("Title", TextFormatting.ESCAPETEXT_ICON)]
        [SDK.InputMetaData("Default", true)]
        public bool Escape { get; set; }

        [SDK.InputValue(nameof(Text))]
        [SDK.InputMetaData("Title", "Text")]
        public String Text { get; set; }

        protected override String Process(String value)
        {
            var newText = Text.SanitizeUserInput(Escape);

            if (string.IsNullOrEmpty(value)) return newText;

            return value + newText;
        }
    }

    [SDK.ContentNode("PrependTextFunction")]
    [SDK.ContentMetaData("Title", "Prepend")]
    public sealed class PrependTextFunction : TextFunction
    {
        [SDK.InputValue(nameof(Escape))]
        [SDK.InputMetaData("Group", TextFormatting.INVISIBLE_GROUP), SDK.InputMetaData("Title", TextFormatting.ESCAPETEXT_ICON)]
        // [SDK.InputMetaData("DockToLeftOf", nameof(Text))]
        [SDK.InputMetaData("Default", true)]
        public bool Escape { get; set; }

        [SDK.InputValue(nameof(Text))]
        [SDK.InputMetaData("Title", "Text")]
        public String Text { get; set; }

        protected override String Process(String value)
        {
            var newText = Text.SanitizeUserInput(Escape);

            if (string.IsNullOrEmpty(value)) return newText;

            return newText + value;
        }
    }

    [SDK.ContentNode("ReplaceTextFunction")]
    [SDK.ContentMetaData("Title", "Replace")]
    public sealed class ReplaceTextFunction : TextFunction
    {
        [SDK.InputValue(nameof(OldEscape))]
        [SDK.InputMetaData("Group", "Old"), SDK.InputMetaData("Title", "Esc")]
        [SDK.InputMetaData("Default", true)]
        public bool OldEscape { get; set; }

        [SDK.InputValue(nameof(OldString))]
        [SDK.InputMetaData("Group", "Old"), SDK.InputMetaData("Title", "Text")]
        public String OldString { get; set; }

        [SDK.InputValue(nameof(NewEscape))]
        [SDK.InputMetaData("Group", "New"), SDK.InputMetaData("Title", "Esc")]
        [SDK.InputMetaData("Default", true)]
        public bool NewEscape { get; set; }

        [SDK.InputValue(nameof(NewString))]
        [SDK.InputMetaData("Group", "New"), SDK.InputMetaData("Title", "Text")]
        public String NewString { get; set; }

        protected override String Process(String value)
        {
            return value?.Replace(OldString.SanitizeUserInput(OldEscape), NewString.SanitizeUserInput(NewEscape));
        }
    }

    [SDK.ContentNode("TrimTextFunction")]
    [SDK.ContentMetaData("Title", "Trim")]
    public sealed class TrimTextFunction : TextFunction
    {
        [SDK.InputValue(nameof(TrimStart))]
        [SDK.InputMetaData("Group", TextFormatting.INVISIBLE_GROUP), SDK.InputMetaData("Title", "Start")]
        [SDK.InputMetaData("Default", true)]
        public Boolean TrimStart { get; set; }

        [SDK.InputValue(nameof(TrimEnd))]
        [SDK.InputMetaData("Group", TextFormatting.INVISIBLE_GROUP), SDK.InputMetaData("Title", "End")]
        [SDK.InputMetaData("Default", true)]
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
    [SDK.ContentMetaData("Title", "Insert At")]
    public sealed class InsertTextAtFunction : TextFunction
    {
        [SDK.InputValue(nameof(NewEscape))]
        [SDK.InputMetaData("Group", "Text to Insert"), SDK.InputMetaData("Title", TextFormatting.ESCAPETEXT_ICON)]
        [SDK.InputMetaData("Default", true)]
        public bool NewEscape { get; set; }

        [SDK.InputValue(nameof(NewString))]
        [SDK.InputMetaData("Group", "Text to Insert"), SDK.InputMetaData("Title", "Text")]
        public String NewString { get; set; }

        [SDK.InputValue(nameof(Offset))]
        [SDK.InputMetaData("Title", "From Start")]
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
    [SDK.ContentMetaData("Title", "Case")]
    public sealed class ChangeTextCaseFunction : TextFunction
    {
        [SDK.InputValue(nameof(CaseType))]
        [SDK.InputMetaData("Title", "Case")]
        [SDK.InputMetaData("ViewStyle", "ComboBox")]
        [SDK.InputMetaData("Default", "Upper")]
        [SDK.InputMetaData("Values", new string[] { "Upper", "Lower", "Title" })]
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
