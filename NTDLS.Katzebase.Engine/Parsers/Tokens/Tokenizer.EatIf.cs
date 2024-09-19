namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character is in the given list, returns true if match was found.
        /// </summary>
        public bool EatIf(char[] characters)
        {
            if (_caret < _text.Length && characters.Contains(_text[_caret]))
            {
                _caret++;
                InternalEatWhiteSpace();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character matches the given value, returns true if match was found.
        /// </summary>
        public bool EatIf(char character)
            => EatIf([character]);
    }
}
