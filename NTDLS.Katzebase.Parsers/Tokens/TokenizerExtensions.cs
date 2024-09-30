using NTDLS.Helpers;
using NTDLS.Katzebase.Parsers.Query.Class.Helpers;
using System.Text;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    /// <summary>
    /// Used to walk various types of string and expressions.
    /// </summary>
    public static class TokenizerExtensions
    {
        /// <summary>
        /// Gets the end of the query segment by using the stopAtTokens and the start of the next query, using which ever one comes first.
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <param name="stopAtTokens"></param>
        /// <returns></returns>
        public static int FindEndOfQuerySegment(this Tokenizer tokenizer, string[] stopAtTokens)
        {
            //Find where the join conditions end.
            if (tokenizer.TryGetFirstIndexOf(stopAtTokens, out var nextPartOfQueryCaret) == false)
            {
                nextPartOfQueryCaret = tokenizer.Length; //No end marker found, consume the entire query.

            }
            if (tokenizer.TryFindCompareNext((o) => StaticParserUtility.IsStartOfQuery(o), out var foundToken, out var startOfNextQueryCaret) == false)
            {
                startOfNextQueryCaret = tokenizer.Length; //No end marker found, consume the entire query.
            }

            int?[] carets = [nextPartOfQueryCaret, startOfNextQueryCaret];

            return carets.Min().EnsureNotNull();
        }

        /// <summary>
        /// Splits the given text on a comma delimiter while paying attention to the scope denoted by open and close parentheses..
        /// </summary>
        public static List<string> ScopeSensitiveSplit(this string text, char splitOn)
            => text.ScopeSensitiveSplit(splitOn, '(', ')');

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
        /// Returns true if the text is a valid identifier string, such as a schema name, field name etc.
        /// </summary>
        public static bool IsQueryFieldIdentifier(this string text)
            => string.IsNullOrWhiteSpace(text) == false && text.All(IsQueryIdentifier);

        /// <summary>
        /// Returns true if the text is a valid identifier character, such as a schema name, field name etc.
        /// </summary>
        public static bool IsQueryIdentifier(this char c)
                => char.IsWhiteSpace(c)
                || char.IsLetterOrDigit(c) //Numbers or letters, could be a field name, schema name, function name, etc.
                || c == '_'
                || c == ':' //Schema separators. [Schema1:Schema2].
                || c == '.'; //Schema field-separators. [schemaPrefix.FieldName].

        /// <summary>
        /// Returns true if the text is a valid identifier string.
        /// </summary>
        public static bool IsIdentifier(this string text)
            => string.IsNullOrWhiteSpace(text) == false && text.All(IsIdentifier);

        /// <summary>
        /// Returns true if the text is a valid identifier character.
        /// </summary>
        public static bool IsIdentifier(this char c)
            => char.IsWhiteSpace(c)
                || char.IsLetterOrDigit(c) //Numbers or letters, could be a field name, schema name, function name, etc.
                || c == '_'
                || c == '#' //Temp schema.
                || c == ':' //Schema separators. [Schema1:Schema2].
                || c == '.'; //Schema field-separators. [schemaPrefix.FieldName].
        public static readonly char[] MathematicalCharacters = { '~', '!', '%', '^', '&', '|', '*', '(', ')', '-', '+', '/', '=' };
        /// <summary>
        /// Returns true if the character is a valid mathematical character.
        /// </summary>
        public static bool IsMathematicalOperator(this char c)
            => MathematicalCharacters.Contains(c);

        public static readonly char[] TokenConnectorCharacters = { '%', '^', '&', '|', '*', '-', '+', '/' };
        /// <summary>
        /// Returns true if the character is a valid mathematical character.
        /// </summary>
        public static bool IsTokenConnectorCharacter(this char c)
            => TokenConnectorCharacters.Contains(c);
    }
}
