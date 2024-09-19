namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns true if the tokenizer text contains the given string.
        /// </summary>
        public bool Contains(string givenString)
            => _text.Contains(givenString, StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// Returns true if the tokenizer text contains any of the the given strings.
        /// </summary>
        public bool Contains(string[] givenStrings)
        {
            foreach (var givenString in givenStrings)
            {
                if (_text.Contains(givenString, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
