namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Skips the next token using the standard delimiters.
        /// </summary>
        public void EatNext()
            => EatGetNext(_standardTokenDelimiters, out _);

        /// <summary>
        /// Skips the next token using the given delimiters.
        /// </summary>
        public void EatNext(char[] delimiters)
            => EatGetNext(delimiters, out _);

        /// <summary>
        /// Skips the next token using the given delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public void EatNext(char[] delimiters, out char outStoppedOnDelimiter)
            => EatGetNext(delimiters, out outStoppedOnDelimiter);
    }
}
