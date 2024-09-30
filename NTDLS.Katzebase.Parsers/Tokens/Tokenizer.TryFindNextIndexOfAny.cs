using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Returns the next found index of the of the given characters.
        /// </summary>
        public bool TryFindNextIndexOfAny(char[] characters, [NotNullWhen(true)] out int? foundAtCaret)
        {
            for (int i = Caret; i < _text.Length; i++)
            {
                if (characters.Contains(_text[i]))
                {
                    foundAtCaret = i;
                    return true;
                }
            }

            foundAtCaret = null;
            return false;
        }

        /// <summary>
        /// Returns the next found index of the of the given strings.
        /// </summary>
        public bool TryFindNextIndexOfAny(string[] givenStrings, [NotNullWhen(true)] out int? foundAtCaret)
        {
            foreach (var givenString in givenStrings)
            {
                int index = _text.IndexOf(givenString, Caret, StringComparison.InvariantCultureIgnoreCase);
                if (index >= 0)
                {
                    foundAtCaret = index;
                    return true;
                }
            }

            foundAtCaret = null;
            return false;
        }


        /// <summary>
        /// Returns the next found index of the of the given string.
        /// </summary>
        public bool TryFindNextIndexOfAny(string givenString, [NotNullWhen(true)] out int? foundAtCaret)
        {
            int index = _text.IndexOf(givenString, Caret, StringComparison.InvariantCultureIgnoreCase);
            if (index >= 0)
            {
                foundAtCaret = index;
                return true;
            }
            foundAtCaret = null;
            return false;
        }
    }
}
