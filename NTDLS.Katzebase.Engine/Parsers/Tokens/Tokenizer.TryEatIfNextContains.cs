namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNextContains(string[] givenTokens, char[] delimiters)
            => TryEatCompareNext((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNextContains(string[] givenTokens)
            => TryEatCompareNext((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNextContains(string givenToken, char[] delimiters)
            => TryEatCompareNext((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNextContains(string givenToken)
            => TryEatCompareNext((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNextContains(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryEatCompareNext((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNextContains(string[] givenTokens, out string outFoundToken)
            => TryEatCompareNext((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNextContains(string givenToken, char[] delimiters, out string outFoundToken)
            => TryEatCompareNext((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNextContains(string givenToken, out string outFoundToken)
            => TryEatCompareNext((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);
    }
}
