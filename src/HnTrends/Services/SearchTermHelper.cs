namespace HnTrends.Services
{
    using System;

    internal static class SearchTermHelper
    {
        private static readonly char[] SplitChars = { ' ' };

        public static string MakeAllWordSearch(string text)
        {
            if (text.IndexOf(' ') < 0)
            {
                return text;
            }

            var parts = text.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);

            var result = string.Empty;
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.Length > 1 && part[0] != '+' && part[0] != '-')
                {
                    result += '+';
                }

                result += part;

                if (i < parts.Length - 1)
                {
                    result += ' ';
                }
            }

            return result;
        }
    }
}
