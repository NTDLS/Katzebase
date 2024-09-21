namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns the position of the any of the given characters, seeks from the current caret position.
        /// Throws exception if the character is not found.
        /// </summary>
        public int GetNextIndexOf(char[] characters)
        {
            if (TryGetNextIndexOfAny(characters, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Tokenizer character not found [{string.Join("],[", characters)}].");
        }

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public int GetNextIndexOf(string[] givenStrings)
        {
            if (TryGetNextIndexOfAny(givenStrings, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Expected string not found [{string.Join("],[", givenStrings)}].");
        }

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public int GetNextIndexOf(GetNextIndexOfProc proc)
        {
            if (TryEatCompareNext(proc, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Expected string not found {proc.GetType().Name}.");
        }

        /// <summary>
        /// Returns the index of the given string. Throws exception if not found.
        /// </summary>
        public int GetNextIndexOf(string givenString)
        {
            if (TryGetNextIndexOfAny(givenString, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Expected string not found [{string.Join("],[", givenString)}].");
        }

    }
}
