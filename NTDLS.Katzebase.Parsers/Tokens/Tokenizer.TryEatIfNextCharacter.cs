﻿namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        public bool TryEatIfNextCharacter(char character)
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