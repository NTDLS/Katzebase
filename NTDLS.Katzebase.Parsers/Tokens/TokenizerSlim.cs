using NTDLS.Katzebase.Api.Exceptions;
using System.Runtime.CompilerServices;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    /// <summary>
    /// Lightweight version of Tokenizer, used to walk various types of string and expressions.
    /// </summary>
    public class TokenizerSlim
    {
        #region Private backend variables.

        private readonly string _text;
        private int _caret = 0;
        private readonly char[] _standardTokenDelimiters;

        #endregion

        #region Public properties.

        /// <summary>
        /// Whether the tokenizer should skip a delimiter when it is encountered. Note that whitespace is always skipped.
        /// </summary>
        public bool SkipDelimiter { get; set; } = true;
        public char? NextCharacter => _caret < _text.Length ? _text[_caret] : null;
        public bool IsExhausted() => _caret >= _text.Length;
        public char[] TokenDelimiters => _standardTokenDelimiters;
        public int Caret => _caret;
        public int Length => _text.Length;
        public string Text => _text;

        #endregion

        public void SetCaret(int caret)
        {
            _caret = caret;
        }

        /// <summary>
        /// Creates a tokenizer.
        /// </summary>
        public TokenizerSlim(string text, char[] standardTokenDelimiters)
        {
            _text = text;
            _standardTokenDelimiters = standardTokenDelimiters;
            EatWhiteSpace();
        }

        /// <summary>
        /// Creates a tokenize using only whitespace as a delimiter.
        /// </summary>
        public TokenizerSlim(string text)
        {
            _text = text;
            _standardTokenDelimiters = ['\r', '\n', ' '];
            EatWhiteSpace();
        }

        /// <summary>
        /// Gets the remainder of the text from current caret position.
        /// </summary>
        public string Remainder()
            => _text.Substring(_caret);

        /// <summary>
        /// Skips the next character in the sequence.
        /// </summary>
        /// <exception cref="KbParserException"></exception>
        public void EatNextCharacter()
        {
            if (_caret >= _text.Length)
            {
                throw new Exception("The tokenizer sequence is empty.");
            }

            _caret++;
            EatWhiteSpace();
        }

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string EatGetNext(char[] delimiters)
        {
            var token = string.Empty;

            if (_caret == _text.Length)
            {
                return string.Empty;
            }

            for (; _caret < _text.Length; _caret++)
            {
                if (delimiters.Contains(_text[_caret]) == true)
                {
                    if (SkipDelimiter)
                    {
                        _caret++;
                    }
                    break;
                }

                token += _text[_caret];
            }

            EatWhiteSpace();

            return token.Trim();
        }

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string EatGetNext()
        {
            var token = string.Empty;

            if (_caret == _text.Length)
            {
                return string.Empty;
            }

            for (; _caret < _text.Length; _caret++)
            {
                if (_standardTokenDelimiters.Contains(_text[_caret]) == true)
                {
                    if (SkipDelimiter)
                    {
                        _caret++;
                    }
                    break;
                }

                token += _text[_caret];
            }

            EatWhiteSpace();

            return token.Trim();
        }

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetNext(char[] delimiters, out char? outStoppedAtDelimiter)
        {
            var token = string.Empty;
            int restoreCaret = _caret;

            outStoppedAtDelimiter = null;

            if (_caret == _text.Length)
            {
                return string.Empty;
            }

            for (; _caret < _text.Length; _caret++)
            {
                if (delimiters.Contains(_text[_caret]) == true)
                {
                    outStoppedAtDelimiter = _text[_caret];
                    if (SkipDelimiter)
                    {
                        _caret++;
                    }
                    break;
                }

                token += _text[_caret];
            }

            _caret = restoreCaret;

            return token.Trim();
        }

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string EatGetNext(char[] delimiters, out char? outStoppedAtDelimiter)
        {
            var token = string.Empty;

            outStoppedAtDelimiter = null;

            if (_caret == _text.Length)
            {
                return string.Empty;
            }

            for (; _caret < _text.Length; _caret++)
            {
                if (delimiters.Contains(_text[_caret]) == true)
                {
                    outStoppedAtDelimiter = _text[_caret];
                    if (SkipDelimiter)
                    {
                        _caret++;
                    }
                    break;
                }

                token += _text[_caret];
            }

            EatWhiteSpace();

            return token.Trim();
        }

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string EatGetNext(out char? outStoppedAtDelimiter)
        {
            var token = string.Empty;

            outStoppedAtDelimiter = null;

            if (_caret == _text.Length)
            {
                return string.Empty;
            }

            for (; _caret < _text.Length; _caret++)
            {
                if (_standardTokenDelimiters.Contains(_text[_caret]) == true)
                {
                    outStoppedAtDelimiter = _text[_caret];
                    if (SkipDelimiter)
                    {
                        _caret++;
                    }
                    break;
                }

                token += _text[_caret];
            }

            EatWhiteSpace();

            return token.Trim();
        }

        /// <summary>
        /// Moves the caret past any whitespace, does not record breadcrumb.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EatWhiteSpace()
        {
            while (_caret < _text.Length && char.IsWhiteSpace(_text[_caret]))
            {
                _caret++;
            }
        }

        #region IsNextToken.

        public delegate bool TryNextTokenValidationProc(string peekedToken);

        public delegate bool TryNextTokenComparerProc(string peekedToken, string givenToken);
        public delegate bool TryNextTokenProc(string peekedToken);

        #region TryEatNextTokenEndsWith

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string[] givenTokens, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string[] givenTokens)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string givenToken, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string givenToken)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string[] givenTokens, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string givenToken, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string givenToken, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        #endregion

        #region TryEatNextTokenStartsWith

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string[] givenTokens, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string[] givenTokens)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string givenToken, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string givenToken)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string[] givenTokens, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string givenToken, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string givenToken, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        #endregion

        #region TryEatNextTokenContains

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string[] givenTokens, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string[] givenTokens)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string givenToken, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string givenToken)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string[] givenTokens, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string givenToken, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string givenToken, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        #endregion

        #region TryEatValidateNextToken

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNextToken(TryNextTokenValidationProc validator)
            => TryEatValidateNextToken(validator, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNextToken(TryNextTokenValidationProc validator, char[] delimiters)
            => TryEatValidateNextToken(validator, delimiters, out _);

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNextToken(TryNextTokenValidationProc validator, out string outFoundToken)
            => TryEatValidateNextToken(validator, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNextToken(TryNextTokenValidationProc validator, char[] delimiters, out string outFoundToken)
        {
            int restoreCaret = _caret;
            outFoundToken = EatGetNext(delimiters, out _);

            if (validator(outFoundToken))
            {
                return true;
            }

            _caret = restoreCaret;
            return false;
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens)
            => TryEatCompareNextToken(comparer, givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens, char[] delimiters)
            => TryEatCompareNextToken(comparer, givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens, out string outFoundToken)
            => TryEatCompareNextToken(comparer, givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens, char[] delimiters, out string outFoundToken)
        {
            int restoreCaret = _caret;
            outFoundToken = EatGetNext(delimiters, out _);

            foreach (var givenToken in givenTokens)
            {
                if (comparer(outFoundToken, givenToken))
                {
                    return true;
                }
            }

            _caret = restoreCaret;
            return false;
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string[] givenTokens, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string[] givenTokens)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string givenToken)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string givenToken, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string[] givenTokens, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string givenToken, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string givenToken, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        #endregion

        #endregion

        #region EatGetMatchingScope.

        /// <summary>
        /// Matches scope using open and close parentheses and returns the text between them.
        /// </summary>
        public string EatGetMatchingScope()
            => EatGetMatchingScope('(', ')');

        /// <summary>
        /// Matches scope using the given open and close values and returns the text between them.
        /// </summary>
        public string EatGetMatchingScope(char open, char close)
        {
            int scope = 0;

            EatWhiteSpace();

            if (_text[_caret] != open)
            {
                throw new Exception($"Expected scope character not found [{open}].");
            }

            int startPosition = _caret + 1;

            for (; _caret < _text.Length; _caret++)
            {
                if (_text[_caret] == open)
                {
                    scope++;
                }
                else if (_text[_caret] == close)
                {
                    scope--;
                }

                if (scope < 0)
                {
                    throw new Exception($"Expected scope [{open}] and [{close}] fell below zero.");
                }

                if (scope == 0)
                {
                    var result = _text.Substring(startPosition, _caret - startPosition).Trim();

                    _caret++;
                    EatWhiteSpace();

                    return result;
                }
            }

            throw new Exception($"Expected matching scope not found [{open}] and [{close}], ended at scope [{scope}].");
        }

        #endregion

    }
}
