using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DbSync.Core
{
    static class JsonConverter
    {
        private static string RegexReplace(this string input, string pattern, string replacement)
        {
            return Regex.Replace(input, pattern, replacement);
        }
        public static string ConvertToJson(string data)
        {
            return data
                    .RegexReplace("<root>", "[")
                    .RegexReplace("</root>", "]")
                    .RegexReplace("<row", "{")
                    .RegexReplace(@"(?<="")\B", ",")
                    .RegexReplace(", />", "},")
                    .RegexReplace(@"},\n]", "}\n]")
                    .RegexReplace(@"\b(?<=\W)(?<=[^""])", "\"")
                    .RegexReplace(@"\b(?=\W)(?=[^""])", "\"")
                    .RegexReplace("=", ":");
        }
    }
}
