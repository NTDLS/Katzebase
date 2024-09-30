namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNext(TryNextTokenComparerProc comparer, string[] givenTokens)
            => TryEatCompareNext(comparer, givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNext(TryNextTokenComparerProc comparer, string[] givenTokens, char[] delimiters)
            => TryEatCompareNext(comparer, givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNext(TryNextTokenComparerProc comparer, string[] givenTokens, out string outFoundToken)
            => TryEatCompareNext(comparer, givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNext(TryNextTokenComparerProc comparer, string[] givenTokens, char[] delimiters, out string outFoundToken)
        {
            int restoreCaret = Caret;
            outFoundToken = EatGetNext(delimiters, out _);

            foreach (var givenToken in givenTokens)
            {
                if (comparer(outFoundToken, givenToken))
                {
                    return true;
                }
            }

            Caret = restoreCaret;
            return false;
        }

        /// <summary>
        /// Returns true if the next token causes the given delegate to return true and passes out the index of the found value.
        /// </summary>
        public bool TryEatCompareNext(GetNextIndexOfProc proc, out int foundIndex)
        {
            int restoreCaret = Caret;

            while (IsExhausted() == false)
            {
                int previousCaret = Caret;
                var token = EatGetNext();

                if (proc(token))
                {
                    foundIndex = previousCaret;
                    Caret = restoreCaret;
                    return true;
                }
            }

            foundIndex = -1;
            Caret = restoreCaret;

            return false;
        }
    }
}
