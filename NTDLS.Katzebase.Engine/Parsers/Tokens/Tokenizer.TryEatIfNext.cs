namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character is in the given list, returns true if match was found.
        /// </summary>
        public bool TryEatIfNext(char[] characters)
        {
            if (_caret < _text.Length && characters.Contains(_text[_caret]))
            {
                _caret++;
                InternalEatWhiteSpace();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character is in the given list, returns true if match was found.
        /// </summary>
        public bool TryEatIfNext(char[] characters, out char foundCharacter)
        {
            foundCharacter = _text[_caret];
            if (_caret < _text.Length && characters.Contains(_text[_caret]))
            {
                _caret++;
                InternalEatWhiteSpace();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character matches the given value, returns true if match was found.
        /// </summary>
        public bool TryEatIfNext(char character)
            => TryEatIfNext([character]);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNext(string[] givenTokens, char[] delimiters)
            => TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNext(string[] givenTokens)
            => TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNext(string givenToken)
            => TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNext(string givenToken, char[] delimiters)
            => TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNext(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNext(string[] givenTokens, out string outFoundToken)
            => TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNext(string givenToken, out string outFoundToken)
            => TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIfNext(string givenToken, char[] delimiters, out string outFoundToken)
            => TryEatCompareNext((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);
    }
}
