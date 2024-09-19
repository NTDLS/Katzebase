namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Throws exception if the next character is not in the given array.
        /// </summary>
        public void IsNext(char[] characters)
        {
            if (!TryIsNext(characters, out var foundCharacter))
            {
                throw new Exception($"Invalid token, found [{foundCharacter}], expected [{string.Join("],[", characters)}].");
            }
        }

        /// <summary>
        /// Throws exception if the next character is not the given.
        /// </summary>
        public void IsNext(char character)
        {
            if (!TryIsNext(character, out var foundCharacter))
            {
                throw new Exception($"Invalid token, found [{foundCharacter}], expected [{character}].");
            }
        }

        /// <summary>
        /// Throws exception if the next token is not in the given array, using the given delimiters.
        /// </summary>
        public void IsNext(string[] givenTokens, char[] delimiters)
        {
            if (!TryCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out var outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{string.Join("],[", givenTokens)}].");
            }
        }

        /// <summary>
        /// Throws exception if the next token is not in the given array, using the standard delimiters.
        /// </summary>
        public void IsNext(string[] givenTokens)
        {
            if (!TryCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out var outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{string.Join("],[", givenTokens)}].");
            }
        }
        /// <summary>
        /// Throws exception if the next token is not in the given value, using the standard delimiters.
        /// </summary>
        public void IsNext(string givenToken)
        {
            if (!TryCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out var outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{givenToken}].");
            }
        }
        /// <summary>
        /// Throws exception if the next token is not in the given value, using the given delimiters.
        /// </summary>
        public void IsNext(string givenToken, char[] delimiters)
        {
            if (!TryCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out var outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{givenToken}].");
            }
        }
        /// <summary>
        /// Throws exception if the next token is not in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public void IsNext(string[] givenTokens, char[] delimiters, out string outFoundToken)
        {
            if (!TryCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{string.Join("],[", givenTokens)}].");
            }
        }
        /// <summary>
        /// Throws exception if the next token is not in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public void IsNext(string[] givenTokens, out string outFoundToken)
        {
            if (!TryCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{string.Join("],[", givenTokens)}].");
            }
        }
        /// <summary>
        /// Throws exception if the next token is not in the given value, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public void IsNext(string givenToken, out string outFoundToken)
        {
            if (!TryCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{givenToken}].");
            }
        }
        /// <summary>
        /// Throws exception if the next token is not in the given value, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public void IsNext(string givenToken, char[] delimiters, out string outFoundToken)
        {
            if (!TryCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken))
            {
                throw new Exception($"Invalid token, found [{outFoundToken}], expected [{givenToken}].");
            }
        }
    }
}
