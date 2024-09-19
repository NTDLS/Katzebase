namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns true if the next character that is not a letter/digit/whitespace is in the given array.
        /// </summary>
        public bool IsNextNonIdentifier(char[] c)
            => IsNextNonIdentifier(c, out _);

        /// <summary>
        /// Returns true if the next character that is not a letter/digit/whitespace is in the given array.
        /// </summary>
        public bool IsNextNonIdentifier(char[] c, out int index)
        {
            for (int i = _caret; i < _text.Length; i++)
            {
                if (_text[i].IsQueryIdentifier())
                {
                    continue;
                }
                else if (c.Contains(_text[i]))
                {
                    index = i;
                    return true;
                }
                else
                {
                    index = -1;
                    return false;
                }
            }

            index = -1;
            return false;
        }
    }
}
