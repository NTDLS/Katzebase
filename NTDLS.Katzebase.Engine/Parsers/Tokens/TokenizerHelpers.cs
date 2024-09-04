using System.Text;
using System.Text.RegularExpressions;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    public class TokenizerHelpers
    {
        static public bool IsValidIdentifier(string text)
        {
            var regex = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$");
            var matches = regex.Matches(text);

            if (matches.Count == 1)
            {
                return matches[0].Value == text;
            }

            return false;
        }

        static public bool IsValidIdentifier(string text, char ignoreCharacter)
        {
            return IsValidIdentifier(text, [ignoreCharacter]);
        }

        static public bool IsValidIdentifier(string text, char[] ignoreCharacters)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (char ignore in ignoreCharacters)
            {
                text = text.Replace(ignore.ToString(), "");
            }

            var regex = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$");
            var matches = regex.Matches(text);

            if (matches.Count == 1)
            {
                return matches[0].Value == text;
            }

            return false;
        }

        /// <summary>
        /// Splits the given text on the delimiter while paying attention to the scoped denoted by the given open and close characters.
        /// </summary>
        public static List<string> SplitWhileObeyingScope(string text)
            => SplitWhileObeyingScope(text, ',', '(', ')');

        /// <summary>
        /// Splits the given text on the delimiter while paying attention to the scoped denoted by the given open and close characters.
        /// </summary>
        /// <returns></returns>
        public static List<string> SplitWhileObeyingScope(string text, char splitOn, char open, char close)
        {
            int scope = 0;

            List<string> results = new();

            StringBuilder buffer = new();

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == open)
                {
                    scope++;
                }
                else if (text[i] == close)
                {
                    scope--;
                }

                if (scope == 0 && text[i] == splitOn)
                {
                    results.Add(buffer.ToString().Trim());
                    buffer.Clear();
                }
                else
                {
                    buffer.Append(text[i]);
                }
            }

            if (buffer.Length > 0)
            {
                results.Add(buffer.ToString().Trim());
            }

            return results;
        }
    }
}
