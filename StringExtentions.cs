﻿using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

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

        public static string Sanitise(this string input, bool removeSymbols = false)
        {
            var charsToRemove = removeSymbols
                ? new[]
                {
                    "\t", "ِ", "َ", "ُ", "ّ", "ً", "ٍ",
                    "ْ", "۔", "-", ".", "ٓ", "(", ")",
                    "￿", "ؑ", "٘", "۱", "۲", "۵", "۳", "۴", "۶", "۷", "۸",
                    "۹", "۰", " ب ", " د ", " ہ ", " ء ", " ج ", " الف "
                }
                : new[]
                {
                    "\t", "۔", "-", ".", "ٓ", "(", ")",
                    "￿", "ؑ", "٘", "۱", "۲", "۵", "۳", "۴", "۶", "۷", "۸",
                    "۹", "۰", " ب ", " د ", " ہ ", " ء ", " ج ", " الف "
                };
            foreach (var c in charsToRemove)
            {
                input = input.Replace(c, string.Empty);
            }

            while (input.Contains("  "))
            {
                input = input.Replace("  ", " ");
            }

            input = Regex.Replace(input, " الف$", "");
            input = Regex.Replace(input, " ب$", "");
            input = Regex.Replace(input, " ج$", "");
            input = Regex.Replace(input, " و$", "");
            input = Regex.Replace(input, " د$", "");
            input = Regex.Replace(input, " ہ$", "");
            return input;
        }

        public static string RemoveTabs(this string input)
        {
            return input.Replace("\t", string.Empty);
        }


        public static string RemoveNewLines(this string input)
        {
            return input.Replace("\n", string.Empty)
                        .Replace("\r", string.Empty);
        }
        public static string ConsolidateMiltipleLines(this string input)
        {
            input = input.Replace("\r", string.Empty);
            while (input.Contains("\n\n"))
            {
                input = input.Replace("\n\n", "\n");
            }
            return input;
        }
    }
}
