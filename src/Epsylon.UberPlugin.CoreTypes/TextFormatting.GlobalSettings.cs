using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberPlugin.CoreTypes
{
    using System.Linq;
    using UberFactory;

    using TEXTFUNC = Func<String, String>;

    [SDK.Title("Text Formatting Settings")]
    [SDK.ContentNode("TextFormattingSettings")]    
    public class TextFormattingSettings : SDK.ContentObject
    {
        private static string[] _GetAvailableCultureIdentifiers()
        {
            return System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures)
                .Select(item => item.Name)
                .ToArray();
        }

        private static string _GetDefaultCultureIndentifier() { return _GetAvailableCultureIdentifiers().First(); }

        [SDK.InputValue(nameof(CultureIdentifier))]
        [SDK.Title("Culture")]
        [SDK.ViewStyle("ComboBox")]
        [SDK.MetaDataEvaluate("Default",nameof(_GetDefaultCultureIndentifier))]
        [SDK.MetaDataEvaluate("Values", nameof(_GetAvailableCultureIdentifiers))]
        public string CultureIdentifier { get; set; }

        [SDK.InputNode("PreFormatting", true)]
        [SDK.Title("Pre Formatting")]
        [SDK.ItemsPanel("VerticalList")]
        public TEXTFUNC[] PreFormatting { get; set; }

        [SDK.InputNode("PostFormatting", true)]
        [SDK.Title("Post Formatting")]
        [SDK.ItemsPanel("VerticalList")]
        public TEXTFUNC[] PostFormatting { get; set; }

        public System.Globalization.CultureInfo CurrentCulture
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CultureIdentifier) || CultureIdentifier == "INVARIANT") return System.Globalization.CultureInfo.InvariantCulture;

                return System.Globalization.CultureInfo.GetCultureInfo(CultureIdentifier);
            }
        }


        public String ReadText(SDK.ImportContext stream)
        {
            var value = stream.ReadAllText();

            return value == null ? null : PreFormatting.Process(value);
        }

        public void WriteText(SDK.ExportContext stream, String value)
        {
            value = PostFormatting.Process(value);

            value = value ?? string.Empty;

            stream.WriteAllText(value);
        }
    }

}
