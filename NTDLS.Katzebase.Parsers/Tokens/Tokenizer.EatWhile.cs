namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Moves the caret forward while the character is in the given list, returns the count of skipped.
        /// </summary>
        public int EatWhile(char[] characters)
        {
            int count = 0;
            while (Caret < _text.Length && characters.Contains(_text[Caret]))
            {
                count++;
                Caret++;
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
