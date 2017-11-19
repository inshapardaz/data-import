using System.Globalization;
using System.Net;
using System.Text;

namespace Inshapardaz.DataImport
{
    public static class StringExtentions
    {
        public static string RemoveDoubleSpaces(this string input)
        {
            return input?.Replace("  ", " ");
        }

        public static string TrimBrackets(this string input)
        {
            return input?.Trim('{', '}', '(', ')', '[', '~');
        }

        public static string RemoveBrackets(this string input)
        {
            return input?.Replace("[", string.Empty)
                        ?.Replace("]", string.Empty);
        }

        public static string HtmlDecode(this string input)
        { 
            return WebUtility.HtmlDecode(input);
        }

        public static string RemoveMovements(this string input)
        {
            var result = new StringBuilder();
            foreach (var c in input)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }
            return result.ToString();
        }
    }
}
