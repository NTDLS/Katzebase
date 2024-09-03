using System.Runtime.CompilerServices;

namespace NTDLS.Katzebase.Engine.Parsers
{
    /// <summary>
    /// Used to walk various types of string and expressions.
    /// </summary>
    internal class TokenizerSlim
    {
        #region Private backend variables.

        private readonly string _text;
        private int _caret = 0;
        private readonly char[] _standardTokenDelimiters;

        #endregion

        #region Public properties.

        public char? NextCharacter => _caret < _text.Length ? _text[_caret] : null;
        public bool IsEnd() => _caret >= _text.Length;
        public char[] TokenDelimiters => _standardTokenDelimiters;
        public int Caret => _caret;
        public int Length => _text.Length;
        public string Text => _text;

        #endregion

        /// <summary>
        /// Creates a tokenizer.
        /// </summary>
        public TokenizerSlim(string text, char[] standardTokenDelimiters)
        {
            _text = text;
            _standardTokenDelimiters = standardTokenDelimiters;
        }

        /// <summary>
        /// Creates a tokenize using only whitespace as a delimiter.
        /// </summary>
        public TokenizerSlim(string text)
        {
            _text = text;
            _standardTokenDelimiters = Array.Empty<char>();
        }

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetNext(char[] delimiters)
        {
            var token = string.Empty;

            if (_caret == _text.Length)
            {
                return string.Empty;
            }

            for (; _caret < _text.Length; _caret++)
            {
                if (char.IsWhiteSpace(_text[_caret]) || delimiters.Contains(_text[_caret]) == true)
                {
                    _caret++; //skip the delimiter.
                    break;
                }

                token += _text[_caret];
            }

            SkipWhiteSpace();

            return token.Trim();
        }

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetNext()
        {
            var token = string.Empty;

            if (_caret == _text.Length)
            {
                return string.Empty;
            }

            for (; _caret < _text.Length; _caret++)
            {
                if (char.IsWhiteSpace(_text[_caret]) || _standardTokenDelimiters.Contains(_text[_caret]) == true)
                {
                    _caret++; //skip the delimiter.
                    break;
                }

                token += _text[_caret];
            }

            SkipWhiteSpace();

            return token.Trim();
        }

        /// <summary>
        /// Moves the caret past any whitespace, does not record breadcrumb.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipWhiteSpace()
        {
            while (_caret < _text.Length && char.IsWhiteSpace(_text[_caret]))
            {
                _caret++;
            }
        }
    }
}
