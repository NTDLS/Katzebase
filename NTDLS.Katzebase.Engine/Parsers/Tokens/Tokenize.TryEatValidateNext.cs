namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNext(TryNextTokenValidationProc validator)
            => TryEatValidateNext(validator, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNext(TryNextTokenValidationProc validator, char[] delimiters)
            => TryEatValidateNext(validator, delimiters, out _);

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNext(TryNextTokenValidationProc validator, out string outFoundToken)
            => TryEatValidateNext(validator, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNext(TryNextTokenValidationProc validator, char[] delimiters, out string outFoundToken)
        {
            int restoreCaret = _caret;
            outFoundToken = EatGetNext(delimiters, out _);

            if (validator(outFoundToken))
            {
                return true;
            }

            _caret = restoreCaret;
            return false;
        }
    }
}
