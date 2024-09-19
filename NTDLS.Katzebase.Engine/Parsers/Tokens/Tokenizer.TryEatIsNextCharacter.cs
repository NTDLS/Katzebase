﻿namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        public bool TryEatIsNextCharacter(char character)
        {
            RecordBreadcrumb();
            if (NextCharacter == character)
            {
                _caret++;
                InternalEatWhiteSpace();
                return true;
            }
            return false;
        }
    }
}
