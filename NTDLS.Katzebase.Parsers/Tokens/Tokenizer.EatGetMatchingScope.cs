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
                throw new Exception($"Expected scope character not found [{open}].");
            }

            int startPosition = Caret + 1;

            for (; Caret < _text.Length; Caret++)
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
                    throw new Exception($"Expected scope [{open}] and [{close}] fell below zero.");
                }

                if (scope == 0)
                {
                    var result = _text.Substring(startPosition, Caret - startPosition).Trim();

                    Caret++;
                    InternalEatWhiteSpace();

                    return result;
                }
            }

            throw new Exception($"Expected matching scope not found [{open}] and [{close}], ended at scope [{scope}].");
        }
    }
}
