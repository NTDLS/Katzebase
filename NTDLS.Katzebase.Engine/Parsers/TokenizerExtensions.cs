using System.Text;

namespace NTDLS.Katzebase.Engine.Parsers
{
    /// <summary>
    /// Used to walk various types of string and expressions.
    /// </summary>
    public static class TokenizerExtensions
    {
        /// <summary>
        /// Splits the given text on a comma delimiter while paying attention to the scope denoted by open and close parentheses..
        /// </summary>
        public static List<string> ScopeSensitiveSplit(this string text)
            => text.ScopeSensitiveSplit(',', '(', ')');

        /// <summary>
        /// Splits the given text on the delimiter while paying attention to the scope denoted by the given open and close characters.
        /// </summary>
        /// <returns></returns>
        public static List<string> ScopeSensitiveSplit(this string text, char splitOn, char open, char close)
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

        /// <summary>
        /// Returns true if the text is a valid identifier character.
        /// </summary>
        public static bool IsIdentifier(this string text)
            => text.All(IsIdentifier);

        /// <summary>
        /// Returns true if the character is a valid identifier character.
        /// </summary>
        public static bool IsIdentifier(this char c)
        {
            return
                char.IsWhiteSpace(c)
                || char.IsLetterOrDigit(c) //Numbers or letters, could be a field name, schema name, function name, etc.
                || c == ':' //Schema separators. [Schema1:Schema2].
                || c == '.'; //Schema field-separators. [schemaPrefix.FieldName].
        }


        private static readonly char[] _mathematicalCharacters = { '~', '!', '^', '&', '*', '(', ')', '-', '+', '/', '=' };

        /// <summary>
        /// Returns true if the character is a valid mathematical character.
        /// </summary>
        public static bool IsMathematicalOperator(this char c)
            => _mathematicalCharacters.Contains(c);
    }
}
