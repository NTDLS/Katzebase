using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        private void PreValidate()
        {
        }

        private void PostValidate()
        {
            ValidateParentheses();

            int index = _text.IndexOf('\'');
            if (index > 0)
            {
                throw new KbParserException(GetLineNumber(index), $"Invalid syntax, found: ['].");
            }

            index = _text.IndexOf('\"');
            if (index > 0)
            {
                throw new KbParserException(GetLineNumber(index), $"Invalid syntax, found: [\"].");
            }
        }

        private void ValidateParentheses()
        {
            int parenOpen = 0;
            int parenClose = 0;

            int lastOpen = 0;
            int lastClose = 0;

            for (int i = 0; i < _text.Length; i++)
            {
                if (_text[i] == '(')
                {
                    lastOpen = i;
                    parenOpen++;
                }
                else if (_text[i] == ')')
                {
                    lastClose = i;
                    parenClose++;
                }

                if (parenClose > parenOpen)
                {
                    throw new KbParserException(GetLineNumber(i), $"Parentheses mismatch..");
                }
            }

            if (parenClose != parenOpen)
            {
                throw new KbParserException(GetLineNumber(lastClose), $"Parentheses mismatch..");
            }
        }
    }
}
