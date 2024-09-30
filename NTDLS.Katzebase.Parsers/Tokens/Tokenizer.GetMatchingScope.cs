using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Matches scope using open and close parentheses and returns the text between them.
        /// </summary>
        public string MatchingScope(out int endOfScopeCaret)
            => MatchingScope('(', ')', out endOfScopeCaret);

        /// <summary>
        /// Matches scope using open and close parentheses and returns the text between them.
        /// </summary>
        public string MatchingScope()
            => MatchingScope('(', ')', out _);

        /// <summary>
        /// Matches scope using open and close parentheses and returns the text between them.
        /// </summary>
        public string MatchingScope(char open, char close)
            => MatchingScope('(', ')', out _);

        /// <summary>
        /// Matches scope using the given open and close values and returns the text between them.
        /// </summary>
        public string MatchingScope(char open, char close, out int endOfScopeCaret)
        {
            int scope = 0;

            InternalEatWhiteSpace();

            int caret = Caret;

            while (caret < _text.Length && char.IsWhiteSpace(_text[caret]))
            {
                caret++;
            }

            if (_text[caret] != open)
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{open}], found: [{_text[Caret]}].");
            }

            int startPosition = caret + 1;

            for (; caret < _text.Length; caret++)
            {
                if (_text[caret] == open)
                {
                    scope++;
                }
                else if (_text[caret] == close)
                {
                    scope--;
                }

                if (scope < 0)
                {
                    throw new KbParserException(GetCurrentLineNumber(), $"Scope [{open}] and [{close}] mismatch.");
                }

                if (scope == 0)
                {
                    endOfScopeCaret = caret;
                    return _text.Substring(startPosition, caret - startPosition).Trim();
                }
            }

            throw new KbParserException(GetCurrentLineNumber(), $"Scope [{open}] and [{close}] mismatch.");
        }
    }
}
