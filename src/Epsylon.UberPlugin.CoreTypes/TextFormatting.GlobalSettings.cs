using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberPlugin.CoreTypes
{
    using System.Linq;
    using UberFactory;

    [SDK.ContentNode("TextFormattingSettings")]
    [SDK.ContentMetaData("Title", "Text Formatting Settings")]
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
        [SDK.InputMetaData("Title","Culture")]
        [SDK.InputMetaData("ViewStyle", "ComboBox")]
        [SDK.InputMetaDataEvaluate("Default",nameof(_GetDefaultCultureIndentifier))]
        [SDK.InputMetaDataEvaluate("Values", nameof(_GetAvailableCultureIdentifiers))]
        public string CultureIdentifier { get; set; }


        public System.Globalization.CultureInfo CurrentCulture
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CultureIdentifier) || CultureIdentifier == "INVARIANT") return System.Globalization.CultureInfo.InvariantCulture;

                return System.Globalization.CultureInfo.GetCultureInfo(CultureIdentifier);
            }
        }        
    }

}
