namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Moves the caret past any whitespace, does not record breadcrumb.
        /// </summary>
        private void InternalEatWhiteSpace()
        {
            while (Caret < _length && char.IsWhiteSpace(_text[Caret]))
            {
                Caret++;
            }
        }

        /// <summary>
        /// Moves the caret past any whitespace.
        /// </summary>
        public void EatWhiteSpace()
        {
            RecordBreadcrumb();

            while (Caret < _length && char.IsWhiteSpace(_text[Caret]))
            {
                Caret++;
            }
        }
    }
}
