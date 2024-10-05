using NTDLS.Katzebase.Api.Exceptions;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Matches scope using open and close parentheses and returns the text between them.
        /// </summary>
        public string EatGetMatchingScope()
            => EatGetMatchingScope('(', ')');

        /// <summary>
        /// Matches scope using the given open and close values and returns the text between them.
        /// </summary>
        public string EatGetMatchingScope(char open, char close)
        {
            RecordBreadcrumb();

            int scope = 0;

            InternalEatWhiteSpace();

            if (_text[Caret] != open)
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{open}], found: [{_text[Caret]}].");
            }

            int startPosition = Caret + 1;

            for (; Caret < _length; Caret++)
            {
                if (_text[Caret] == open)
                {
                    scope++;
                }
                else if (_text[Caret] == close)
                {
                    scope--;
                }

                if (scope < 0)
                {
                    throw new KbParserException(GetCurrentLineNumber(), $"Scope [{open}] and [{close}] mismatch.");
                }

                if (scope == 0)
                {
                    var result = _text.Substring(startPosition, Caret - startPosition).Trim();

                    Caret++;
                    InternalEatWhiteSpace();

                    return result;
                }
            }

            throw new KbParserException(GetCurrentLineNumber(), $"Scope [{open}] and [{close}] mismatch.");
        }
    }
}
