﻿namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
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
            => ResolveLiteral(EatGetNext(_standardTokenDelimiters, out _));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters.
        /// </summary>
        public string? EatGetNextEvaluated(char[] delimiters)
            => ResolveLiteral(EatGetNext(delimiters, out _));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string? EatGetNextEvaluated(out char outStoppedOnDelimiter)
            => ResolveLiteral(EatGetNext(_standardTokenDelimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string? EatGetNextEvaluated(char[] delimiters, out char outStoppedOnDelimiter)
            => ResolveLiteral(EatGetNext(delimiters, out outStoppedOnDelimiter));
    }
}
