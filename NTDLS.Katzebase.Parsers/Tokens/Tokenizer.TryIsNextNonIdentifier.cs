using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Returns true if the next character that is not a letter/digit/whitespace is the given value.
        /// </summary>
        public bool TryIsNextNonIdentifier(char c)
            => TryIsNextNonIdentifier([c], out _);

        /// <summary>
        /// Returns true if the next character that is not a letter/digit/whitespace is in the given array.
        /// </summary>
        public bool TryIsNextNonIdentifier(char[] c)
            => TryIsNextNonIdentifier(c, out _);

        /// <summary>
        /// Returns true if the next character that is not a letter/digit/whitespace is the given value.
        /// </summary>
        public bool TryIsNextNonIdentifier(char c, [NotNullWhen(true)] out int? index)
            => TryIsNextNonIdentifier([c], out index);

        /// <summary>
        /// Returns true if the next character that is not a letter/digit/whitespace is in the given array.
        /// </summary>
        public bool TryIsNextNonIdentifier(char[] c, [NotNullWhen(true)] out int? index)
        {
            index = null;

            for (int i = Caret; i < _length; i++)
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
                    return false;
                }
            }

            return false;
        }
    }
}
