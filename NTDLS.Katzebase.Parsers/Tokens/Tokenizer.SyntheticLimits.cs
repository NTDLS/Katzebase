namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        private Stack<int> _syntheticLimits = new Stack<int>();

        /// <summary>
        /// Temporarily sets a new Length for the tokenizer. Revert this change with PopSyntheticLimit().
        /// </summary>
        public void PushSyntheticLimit(int length)
        {
            _syntheticLimits.Push(_length);
            _length = length;
        }

        public void PopSyntheticLimit()
        {
            _length = _syntheticLimits.Pop();
        }
    }
}
