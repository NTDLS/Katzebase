namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Gets the next token using the given delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string EatGetNext(char[] delimiters, out char outStoppedOnDelimiter)
        {
            RecordBreadcrumb();

            outStoppedOnDelimiter = '\0';

            var token = string.Empty;

            if (Caret == _text.Length)
            {
                return string.Empty;
            }

            for (; Caret < _text.Length; Caret++)
            {
                if (delimiters.Contains(_text[Caret]) == true)
                {
                    outStoppedOnDelimiter = _text[Caret];
                    Caret++; //skip the delimiter.
                    break;
                }

                token += _text[Caret];
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
