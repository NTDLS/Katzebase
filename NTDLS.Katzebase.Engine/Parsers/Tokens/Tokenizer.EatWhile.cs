namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Moves the caret forward while the character is in the given list, returns the count of skipped.
        /// </summary>
        public int EatWhile(char[] characters)
        {
            int count = 0;
            while (_caret < _text.Length && characters.Contains(_text[_caret]))
            {
                count++;
                _caret++;
            }
            InternalEatWhiteSpace();
            return count;
        }

        /// <summary>
        /// Moves the caret forward while the character matches the given value, returns the count of skipped.
        /// </summary>
        public int EatWhile(char character)
            => EatWhile([character]);
    }
}
