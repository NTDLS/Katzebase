using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Returns the position of the any of the given characters, seeks from the current caret position.
        /// Throws exception if the character is not found.
        /// </summary>
        public int GetNextIndexOf(char[] characters)
        {
            if (TryFindNextIndexOfAny(characters, out var foundIndex))
            {
                return foundIndex.EnsureNotNull();
            }

            throw new KbParserException(GetCurrentLineNumber(), $"Expected [{string.Join("],[", characters)}], found: [{NextCharacter}].");
        }

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public int GetNextIndexOf(string[] givenStrings)
        {
            if (TryFindNextIndexOfAny(givenStrings, out var foundIndex))
            {
                return foundIndex.EnsureNotNull();
            }

            throw new KbParserException(GetCurrentLineNumber(), $"Expected [{string.Join("],[", givenStrings)}].");
        }

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public int GetNextIndexOf(GetNextIndexOfProc proc)
        {
            if (TryEatCompareNext(proc, out var foundIndex))
            {
                return foundIndex.EnsureNotNull();
            }

            throw new KbParserException(GetCurrentLineNumber(), $"Expected [{proc.GetType().Name}].");
        }

        /// <summary>
        /// Returns the index of the given string. Throws exception if not found.
        /// </summary>
        public int GetNextIndexOf(string givenString)
        {
            if (TryFindNextIndexOfAny(givenString, out var foundIndex))
            {
                return foundIndex.EnsureNotNull();
            }

            throw new KbParserException(GetCurrentLineNumber(), $"Expected [{string.Join("],[", givenString)}].");
        }

    }
}
