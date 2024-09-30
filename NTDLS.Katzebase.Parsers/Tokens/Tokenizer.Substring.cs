namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Gets the remainder of the tokenizer text from the internal caret position to the given absolute position.
        /// </summary>
        public string EatRemainder()
        {
            RecordBreadcrumb();

            var result = _text.Substring(Caret);
            Caret = _length;

            InternalEatWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position for a given length.
        /// </summary>
        public string EatSubstring(int length)
        {
            RecordBreadcrumb();

            var result = _text.Substring(Caret, length);
            Caret += length;

            InternalEatWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position to the given absolute position.
        /// </summary>
        public string SubStringAbsolute(int absoluteEndPosition)
        {
            var result = _text.Substring(Caret, absoluteEndPosition - Caret);
            return result;
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position to the given absolute position.
        /// </summary>
        public string EatSubStringAbsolute(int absoluteEndPosition)
        {
            RecordBreadcrumb();

            var result = _text.Substring(Caret, absoluteEndPosition - Caret);
            Caret = absoluteEndPosition;

            InternalEatWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position to the given absolute position.
        /// </summary>
        public string EatSubstring(int startPosition, int length)
        {
            RecordBreadcrumb();

            var result = _text.Substring(startPosition, length);
            Caret = startPosition + length;

            InternalEatWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets a substring from the tokenizer.
        /// </summary>
        public string Substring(int startPosition, int length)
            => _text.Substring(startPosition, length);

        /// <summary>
        /// Gets the remainder of the text from the given position.
        /// </summary>
        public string RemainderFrom(int startPosition)
            => _text.Substring(startPosition);

        /// <summary>
        /// Gets the remainder of the text from current caret position.
        /// </summary>
        public string Remainder()
            => _text.Substring(Caret);

    }
}
