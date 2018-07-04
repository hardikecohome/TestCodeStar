using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Common.Helpers
{
    public static class StringHelper
    {
        public static Dictionary<string, string> Foreign_characters = new Dictionary<string, string>
        {
            {"À", "A"},
            {"à", "a"},
            {"Â", "A"},
            {"â", "a"},
            {"Æ", "Ae"},//  To verify with Sali
            {"æ", "ae"},//  To verify with Sali
            {"Ç", "C"},
            {"ç", "c"},
            {"É", "E"},
            {"é", "e"},
            {"È", "E"},
            {"è", "e"},
            {"Ê", "E"},
            {"ê", "e"},
            {"Ë", "E"},
            {"ë", "e"},
            {"Î", "I"},
            {"î", "i"},
            {"Ï", "I"},
            {"ï", "i"},
            {"Ô", "O"},
            {"ô", "o"},
            {"Œ", "E"},// To verify with Sali
            {"œ", "e"},// To verify with Sali
            {"Ù", "U"},
            {"ù", "u"},
            {"Û", "U"},
            {"û", "u"},
            {"Ü", "U"},
            {"ü", "u"},
            {"Ÿ", "Y"},
            {"ÿ", "y"}
        };

        public static string ConcatWithComma(this IEnumerable<string> values)
        {
            if (values == null) { return null; }
            var stb = new StringBuilder();
            var i = 0;
            foreach (var str in values)
            {
                if (i != 0) { stb.Append(", "); }
                stb.Append(str);
                i++;
            }
            return stb.ToString();
        }

        public static string MapFrenchSymbols(this string str, bool execute)
        {
            if (execute)
            {
                foreach (KeyValuePair<string, string> entry in Foreign_characters)
                {
                    if (str.Contains(entry.Key))
                    {
                        str = str.Replace(entry.Key, entry.Value);
                    }
                }
            }
            return str;
        }

        public static bool IsFrenchSymbols(this string str)
        {
            var result = false;
            foreach (KeyValuePair<string, string> entry in Foreign_characters)
            {
                if (str.Contains(entry.Key))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
}
