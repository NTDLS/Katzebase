namespace ParserV2.Expression
{
    /// <summary>
    /// Used to walk various types of string and expressions.
    /// </summary>
    internal class Tokenizer
    {
        #region Rules and Convention.
        /*
         * Functions that DO NOT modify the internal tokenizer position should be prefixed with "Inert".
         * Functions that DO NOT throw exceptions should be prefixed with "Try".
         * Functions that are not prefixed with "Try" should throw exceptions if they do not find/seek what they are intended to do.
         * Functions that DO NOT modify the internal tokenizer and DO NOT throw exceptions should be prefixed with "InertTry".
         */
        #endregion

        private readonly  string _text;
        private int _caret = 0;

        public Tokenizer(string text)
        {
            _text = text;
        }

        /// <summary>
        /// Returns true if the tokenizer text contains the given string.
        /// </summary>
        public bool InertContains(string givenString)
            => _text.Contains(givenString, StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// Returns true if the tokenizer text contains any of the the given strings.
        /// </summary>
        public bool InertContains(string[] givenStrings)
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

        /// <summary>
        /// Returns the position of the any of the given characters, seeks from the current internal position.
        /// </summary>
        public bool InertTryGetNextIndexOf(char[] characters, out int position)
        {
            int restorePosition = _caret;

            for (int i = _caret; i < _text.Length; i++)
            {
                if (characters.Contains(_text[i]))
                {
                    position = i;
                    return true;
                }
            }

            position = -1;
            return false;
        }

        /// <summary>
        /// Returns the position of the any of the given characters, seeks from the current internal position.
        /// Throws exception if the character is not found.
        /// </summary>
        public int InertGetNextIndexOf(char[] characters)
        {
            for (int i = _caret; i < _text.Length; i++)
            {
                if (characters.Contains(_text[i]))
                {
                    int index = _caret;
                    return index;
                }
            }

            throw new Exception($"Tokenizer character not found [{string.Join("],[", characters)}].");
        }

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public int InertGetNextIndexOf(string[] givenStrings)
        {
            foreach (var givenString in givenStrings)
            {
                int index = _text.IndexOf(givenString, _caret, StringComparison.InvariantCultureIgnoreCase);
                if (index >= 0)
                {
                    return index;
                }
            }

            throw new Exception($"Expected string not found [{string.Join("],[", givenStrings)}].");
        }

        /// <summary>
        /// Returns the index of the given string. Throws exception if not found.
        /// </summary>
        public int InertGetNextIndexOf(string givenString)
        {
            int index = _text.IndexOf(givenString, _caret, StringComparison.InvariantCultureIgnoreCase);
            if (index >= 0)
            {
                return index;
            }


            throw new Exception($"Expected string not found [{string.Join("],[", givenString)}].");
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position to the given absolute position.
        /// </summary>
        public string SubString(int absoluteEndPosition)
        {
            var result = _text.Substring(_caret, absoluteEndPosition - _caret);
            _caret = absoluteEndPosition;
            return result;
        }

    }
}
