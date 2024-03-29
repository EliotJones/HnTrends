﻿namespace HnTrends.Services
{
    using System;
    using System.Linq;

    internal static class SearchTermHelper
    {
        private static readonly char[] SplitChars = { ' ' };

        public static string MakeSafeWordSearch(string text, bool areAllWordsRequired)
        {
            text = text.Replace("\"", "\"\"");

            if (text.IndexOf(' ') < 0)
            {
                return $"\"{text}\"";
            }

            var parts = text.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);

            return string.Join(areAllWordsRequired ? " " : " OR ", parts.Select(x => $"\"{x}\""));
        }
    }
}
