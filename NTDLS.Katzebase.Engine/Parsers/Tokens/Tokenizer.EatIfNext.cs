namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string[] givenTokens, char[] delimiters)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out var outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{string.Join("],[", givenTokens)}].");
            }
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string[] givenTokens)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out var outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{string.Join("],[", givenTokens)}].");
            }

        }

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string givenToken)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out var outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{givenToken}].");
            }
        }

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string givenToken, char[] delimiters)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out var outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{givenToken}].");
            }
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string[] givenTokens, char[] delimiters, out string outFoundToken)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{string.Join("],[", givenTokens)}].");
            }
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string[] givenTokens, out string outFoundToken)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{string.Join("],[", givenTokens)}].");
            }
        }

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string givenToken, out string outFoundToken)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{givenToken}].");
            }
        }

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string givenToken, char[] delimiters, out string outFoundToken)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{givenToken}].");
            }
        }
    }
}
