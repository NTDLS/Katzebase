namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Allows for enumeration, stopping at a given delimiter while respecting scope. This means that we can split on a
        /// comma, but only if that comma is outside of the scope characters denoted by the supplied open and close characters.
        /// </summary>
        /// <param name="splitOn">Character to split on.</param>
        /// <param name="open">Scope open character.</param>
        /// <param name="close">Scope close character.</param>
        /// <param name="endAtCaret">The absolute caret position to stop parsing.</param>
        /// <returns></returns>
        public ScopeSensitiveSplitter EatScopeSensitiveSplit(char splitOn, char open, char close, int endAtCaret)
            => new ScopeSensitiveSplitter(this, splitOn, open, close, endAtCaret);

        /// <summary>
        /// Allows for enumeration, stopping at a given delimiter while respecting scope. This means that we can split on a
        /// comma, but only if that comma is outside of the scope characters denoted by the supplied open and close characters.
        /// </summary>
        /// <param name="splitOn">Character to split on.</param>
        /// <param name="endAtCaret">The absolute caret position to stop parsing.</param>
        /// <returns></returns>
        public ScopeSensitiveSplitter EatScopeSensitiveSplit(char splitOn, int endAtCaret)
            => new ScopeSensitiveSplitter(this, splitOn, '(', ')', endAtCaret);

        /// <summary>
        /// Allows for enumeration, stopping at a given delimiter while respecting scope. This means that we can split on a
        /// comma, but only if that comma is outside of the scope characters denoted by the supplied open and close characters.
        /// </summary>
        /// <param name="endAtCaret">The absolute caret position to stop parsing.</param>
        /// <returns></returns>
        public ScopeSensitiveSplitter EatScopeSensitiveSplit(int endAtCaret)
            => new ScopeSensitiveSplitter(this, ',', '(', ')', endAtCaret);
    }
}
