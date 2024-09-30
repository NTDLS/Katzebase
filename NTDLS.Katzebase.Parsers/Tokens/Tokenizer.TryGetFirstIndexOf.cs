using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Returns the first index (minimum value) of the found of the given strings.
        /// </summary>
        public bool TryGetFirstIndexOf(string[] givenStrings, [NotNullWhen(true)] out int? foundIndex)
        {
            var indexes = new List<int>();

            foreach (var givenString in givenStrings)
            {
                int index = _text.IndexOf(givenString, Caret, StringComparison.InvariantCultureIgnoreCase);
                if (index >= 0)
                {
                    indexes.Add(index);
                }
            }

            if (indexes.Count > 0)
            {
                foundIndex = indexes.Min();
                return true;
            }

            foundIndex = null;
            return false;
        }


        /// <summary>
        /// Returns the first index (minimum value) of the found of the given string.
        /// </summary>
        public bool TryGetFirstIndexOf(string givenString, [NotNullWhen(true)] out int? foundIndex)
        {
            var indexes = new List<int>();

            int index = _text.IndexOf(givenString, Caret, StringComparison.InvariantCultureIgnoreCase);
            if (index >= 0)
            {
                indexes.Add(index);
            }

            if (indexes.Count > 0)
            {
                foundIndex = indexes.Min();
                return true;
            }

            foundIndex = null;
            return false;
        }
    }
}
