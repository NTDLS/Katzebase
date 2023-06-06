using System.Text.RegularExpressions;

namespace Katzebase.Engine.Query.Tokenizers
{
    public class TokenHelpers
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

        static public bool IsValidIdentifier(string text, string ignoreCharacters)
        {
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
    }
}
