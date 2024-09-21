namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns the position of the any of the given characters, seeks from the current caret position.
        /// </summary>
        public bool TryGetNextIndexOfAny(char[] characters, out int foundIndex)
        {
            for (int i = _caret; i < _text.Length; i++)
            {
                if (characters.Contains(_text[i]))
                {
                    foundIndex = i;
                    return true;
                }
            }

            foundIndex = -1;
            return false;
        }

        /// <summary>
        /// Returns the index of the first found of the given strings.
        /// </summary>
        public bool TryGetNextIndexOfAny(string[] givenStrings, out int foundIndex)
        {
            foreach (var givenString in givenStrings)
            {
                int index = _text.IndexOf(givenString, _caret, StringComparison.InvariantCultureIgnoreCase);
                if (index >= 0)
                {
                    foundIndex = index;
                    return true;
                }
            }

            foundIndex = -1;
            return false;
        }


        /// <summary>
        /// Returns the index of the given string.
        /// </summary>
        public bool TryGetNextIndexOfAny(string givenString, out int foundIndex)
        {
            int index = _text.IndexOf(givenString, _caret, StringComparison.InvariantCultureIgnoreCase);
            if (index >= 0)
            {
                foundIndex = index;
                return true;
            }
            foundIndex = -1;
            return false;
        }
    }
}
