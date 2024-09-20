namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Matches scope using open and close parentheses and skips the entire scope.
        /// </summary>
        public string EatMatchingScope()
            => EatGetMatchingScope('(', ')');

        /// <summary>
        /// Matches scope using the given open and close values and skips the entire scope.
        /// </summary>
        public string EatMatchingScope(char open, char close)
            => EatGetMatchingScope(open, close);

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

            if (_text[_caret] != open)
            {
                throw new Exception($"Expected scope character not found [{open}].");
            }

            int startPosition = _caret + 1;

            for (; _caret < _text.Length; _caret++)
            {
                if (_text[_caret] == open)
                {
                    scope++;
                }
                else if (_text[_caret] == close)
                {
                    scope--;
                }

                if (scope < 0)
                {
                    throw new Exception($"Expected scope [{open}] and [{close}] fell below zero.");
                }

                if (scope == 0)
                {
                    var result = _text.Substring(startPosition, _caret - startPosition).Trim();

                    _caret++;
                    InternalEatWhiteSpace();

                    return result;
                }
            }

            throw new Exception($"Expected matching scope not found [{open}] and [{close}], ended at scope [{scope}].");
        }
    }
}
