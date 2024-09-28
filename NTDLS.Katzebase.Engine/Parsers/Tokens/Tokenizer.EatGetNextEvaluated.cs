namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer<TData> where TData : IStringable
    {
        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters.
        /// </summary>
        public T? EatGetNextEvaluated<T>(char[] delimiters)
            => Helpers.Converters.ConvertToNullable<T>(EatGetNextEvaluated(delimiters));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters.
        /// </summary>
        public T? EatGetNextEvaluated<T>()
            => Helpers.Converters.ConvertToNullable<T>(EatGetNextEvaluated(_standardTokenDelimiters));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters.
        /// </summary>
        public string? EatGetNextEvaluated()
            => ResolveLiteral(EatGetNext(_standardTokenDelimiters, out _)).ToT<string>();

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters.
        /// </summary>
        public string? EatGetNextEvaluated(char[] delimiters)
            => ResolveLiteral(EatGetNext(delimiters, out _)).ToT<string>();

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string? EatGetNextEvaluated(out char outStoppedOnDelimiter)
            => ResolveLiteral(EatGetNext(_standardTokenDelimiters, out outStoppedOnDelimiter)).ToT<string>();

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string? EatGetNextEvaluated(char[] delimiters, out char outStoppedOnDelimiter)
            => ResolveLiteral(EatGetNext(delimiters, out outStoppedOnDelimiter)).ToT<string>();
    }
}
