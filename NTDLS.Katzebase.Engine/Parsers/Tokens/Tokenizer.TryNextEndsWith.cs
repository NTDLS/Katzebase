namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextEndsWith(string[] givenTokens, char[] delimiters)
            => TryCompareNext((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextEndsWith(string[] givenTokens)
            => TryCompareNext((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextEndsWith(string givenToken, char[] delimiters)
            => TryCompareNext((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextEndsWith(string givenToken)
            => TryCompareNext((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextEndsWith(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryCompareNext((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextEndsWith(string[] givenTokens, out string outFoundToken)
            => TryCompareNext((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextEndsWith(string givenToken, char[] delimiters, out string outFoundToken)
            => TryCompareNext((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextEndsWith(string givenToken, out string outFoundToken)
            => TryCompareNext((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);
    }
}
