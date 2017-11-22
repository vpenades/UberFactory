using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberPlugin
{
    using TEXTFUNC = Func<String, String>;

    static class TextFormatting
    {
        public const string INVISIBLE_GROUP = "$"; // this must be a character that enables grouping but resolves to empty

        public const string ENABLED_ICON = "▶"; // 🔲🔳✅❎⬜☑✔✖❌⭕ ⚪⚫
        public const string DISABLED_ICON = "◼";

        public const string ESCAPETEXT_ICON = "Esc";

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
}
