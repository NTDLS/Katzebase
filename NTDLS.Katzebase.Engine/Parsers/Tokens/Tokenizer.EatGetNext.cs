namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Gets the next token using the given delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string EatGetNext(char[] delimiters, out char outStoppedOnDelimiter)
        {
            RecordBreadcrumb();

            outStoppedOnDelimiter = '\0';

            var token = string.Empty;

            if (_caret == _text.Length)
            {
                return string.Empty;
            }

            for (; _caret < _text.Length; _caret++)
            {
                if (delimiters.Contains(_text[_caret]) == true)
                {
                    outStoppedOnDelimiter = _text[_caret];
                    _caret++; //skip the delimiter.
                    break;
                }

                token += _text[_caret];
            }

            InternalEatWhiteSpace();

            return token.Trim();
        }

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        public T EatGetNext<T>()
            => Helpers.Converters.ConvertTo<T>(EatGetNext(_standardTokenDelimiters, out _));

        /// <summary>
        /// Gets the next token using the standard delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public T EatGetNext<T>(out char outStoppedOnDelimiter)
            => Helpers.Converters.ConvertTo<T>(EatGetNext(_standardTokenDelimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        public T EatGetNext<T>(char[] delimiters)
            => Helpers.Converters.ConvertTo<T>(EatGetNext(delimiters, out _));

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        public string EatGetNext()
            => EatGetNext(_standardTokenDelimiters, out _);

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        public string EatGetNext(char[] delimiters)
            => EatGetNext(delimiters, out _);

        /// <summary>
        /// Gets the next token using the given delimiters,
        /// returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public T EatGetNext<T>(char[] delimiters, out char outStoppedOnDelimiter)
            => Helpers.Converters.ConvertTo<T>(EatGetNext(delimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token using the standard delimiters,
        /// returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string EatGetNext(out char outStoppedOnDelimiter)
            => EatGetNext(_standardTokenDelimiters, out outStoppedOnDelimiter);

        /// <summary>
        /// Skips the next token using the standard delimiters,
        /// returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public void EatNext(out char outStoppedOnDelimiter)
            => EatGetNext(_standardTokenDelimiters, out outStoppedOnDelimiter);

    }
}
