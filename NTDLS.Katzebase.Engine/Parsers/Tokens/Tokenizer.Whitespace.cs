namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer<TData> where TData : IStringable
    {
        /// <summary>
        /// Moves the caret past any whitespace, does not record breadcrumb.
        /// </summary>
        private void InternalEatWhiteSpace()
        {
            while (_caret < _text.Length && char.IsWhiteSpace(_text[_caret]))
            {
                _caret++;
            }
        }

        /// <summary>
        /// Moves the caret past any whitespace.
        /// </summary>
        public void EatWhiteSpace()
        {
            RecordBreadcrumb();

            while (_caret < _text.Length && char.IsWhiteSpace(_text[_caret]))
            {
                _caret++;
            }
        }
    }
}
