using System;
using System.Collections.Generic;
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
            return input?.Trim('{', '}', '(', ')', '[');
        }

        public static string RemoveBrackets(this string input)
        {
            return input?.Replace("[", string.Empty)
                        ?.Replace("]", string.Empty);
        }
    }
}
