namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns true if the given strings is found.
        /// </summary>
        public bool TryGetNextIndexOf(GetNextIndexOfProc proc, out int foundIndex)
        {
            int restoreCaret = _caret;

            while (IsExhausted() == false)
            {
                int previousCaret = _caret;
                var token = EatGetNext();

                if (proc(token))
                {
                    foundIndex = previousCaret;
                    _caret = restoreCaret;
                    return true;
                }
            }

            foundIndex = -1;
            _caret = restoreCaret;

            return false;
        }

        /// <summary>
        /// Returns the position of the any of the given characters, seeks from the current caret position.
        /// </summary>
        public bool TryGetNextIndexOf(char[] characters, out int foundIndex)
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
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public bool TryGetNextIndexOf(string[] givenStrings, out int foundIndex)
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
        /// Returns the index of the given string. Throws exception if not found.
        /// </summary>
        public bool TryGetNextIndexOf(string givenString, out int foundIndex)
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
