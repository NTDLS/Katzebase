namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters.
        /// </summary>
        public T? EatGetNextResolved<T>(char[] delimiters)
            => Helpers.Converters.ConvertToNullable<T>(EatGetNextResolved(delimiters));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters.
        /// </summary>
        public T? EatGetNextResolved<T>()
            => Helpers.Converters.ConvertToNullable<T>(EatGetNextResolved(_standardTokenDelimiters));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters.
        /// </summary>
        public string? EatGetNextResolved()
            => ResolveLiteral(EatGetNext(_standardTokenDelimiters, out _));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters.
        /// </summary>
        public string? EatGetNextResolved(char[] delimiters)
            => ResolveLiteral(EatGetNext(delimiters, out _));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string? EatGetNextResolved(out char outStoppedOnDelimiter)
            => ResolveLiteral(EatGetNext(_standardTokenDelimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string? EatGetNextResolved(char[] delimiters, out char outStoppedOnDelimiter)
            => ResolveLiteral(EatGetNext(delimiters, out outStoppedOnDelimiter));
    }
}
