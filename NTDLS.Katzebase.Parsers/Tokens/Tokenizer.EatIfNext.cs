using NTDLS.Katzebase.Api.Exceptions;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character is in the given list.
        /// </summary>
        public void EatIfNext(char[] characters)
        {
            if (!TryEatIfNext(characters, out var outFoundToken))
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{string.Join("],[", characters)}], found: [{outFoundToken}].");
            }
        }

        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character matches the given value.
        /// </summary>
        public void EatIfNext(char character)
        {
            if (!TryEatIfNext([character], out var outFoundToken))
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{character}], found: [{outFoundToken}].");
            }
        }

        /// <summary>
        /// Throws exception if the next token is not in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string[] givenTokens, char[] delimiters)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out var outFoundToken))
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{string.Join("],[", givenTokens)}], found: [{outFoundToken}].");
            }
        }

        /// <summary>
        /// Throws exception if the next token is not in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string[] givenTokens)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out var outFoundToken))
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{string.Join("],[", givenTokens)}], found: [{outFoundToken}].");
            }
        }

        /// <summary>
        /// Throws exception if the next token is not the given value, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string givenToken)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out var outFoundToken))
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{givenToken}], found: [{outFoundToken}].");
            }
        }

        /// <summary>
        /// Throws exception if the next token is not the given value, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string givenToken, char[] delimiters)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out var outFoundToken))
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{givenToken}], found: [{outFoundToken}].");
            }
        }

        /// <summary>
        /// Throws exception if the next token is not in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string[] givenTokens, char[] delimiters, out string outFoundToken)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken))
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{string.Join("],[", givenTokens)}], found: [{outFoundToken}].");
            }
        }

        /// <summary>
        /// Throws exception if the next token is not in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string[] givenTokens, out string outFoundToken)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken))
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{string.Join("],[", givenTokens)}], found: [{outFoundToken}].");
            }
        }

        /// <summary>
        /// Throws exception if the next token is not the given value, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string givenToken, out string outFoundToken)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken))
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{givenToken}], found: [{outFoundToken}].");
            }
        }

        /// <summary>
        /// Throws exception if the next token is not the given value, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public void EatIfNext(string givenToken, char[] delimiters, out string outFoundToken)
        {
            if (!TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken))
            {
                throw new KbParserException(GetCurrentLineNumber(), $"Expected [{givenToken}], found: [{outFoundToken}].");
            }
        }
    }
}
