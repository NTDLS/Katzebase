namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
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
            int restoreCaret = _caret;
            outFoundToken = EatGetNext(delimiters, out _);

            foreach (var givenToken in givenTokens)
            {
                if (comparer(outFoundToken, givenToken))
                {
                    return true;
                }
            }

            _caret = restoreCaret;
            return false;
        }

        /// <summary>
        /// Returns true if the next token causes the given delegate to return true and passes out the index of the found value.
        /// </summary>
        public bool TryEatCompareNext(GetNextIndexOfProc proc, out int foundIndex)
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
    }
}
