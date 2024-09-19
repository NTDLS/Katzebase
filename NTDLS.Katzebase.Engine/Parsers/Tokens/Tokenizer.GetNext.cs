namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        public T GetNext<T>()
            => Helpers.Converters.ConvertTo<T>(GetNext(_standardTokenDelimiters));

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        public T GetNext<T>(char[] delimiters)
            => Helpers.Converters.ConvertTo<T>(GetNext(delimiters));

        /// <summary>
        /// Returns the next token without moving the caret.
        /// </summary>
        public string GetNext()
            => GetNext(_standardTokenDelimiters);

        /// <summary>
        /// Returns the next token without moving the caret using the given delimiters.
        /// </summary>
        public string GetNext(char[] delimiters)
        {
            int restoreCaret = _caret;
            var token = EatGetNext(delimiters, out _);
            _caret = restoreCaret;
            return token;
        }

        /// <summary>
        /// Returns the next token without moving the caret using the given delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public string GetNext(char[] delimiters, out char outStoppedOnDelimiter)
        {
            int restoreCaret = _caret;
            var token = EatGetNext(delimiters, out outStoppedOnDelimiter);
            _caret = restoreCaret;
            return token;
        }
    }
}
