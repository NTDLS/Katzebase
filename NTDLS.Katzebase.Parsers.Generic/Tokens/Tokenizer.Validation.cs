using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer<TData> where TData : IStringable
    {
        private void PreValidate()
        {
            ValidateParentheses();
        }

        private void PostValidate()
        {
            int index = _text.IndexOf('\'');
            if (index > 0)
            {
                throw new KbParserException($"Invalid syntax at ['] at position: [{index:n0}]");
            }

            index = _text.IndexOf('\"');
            if (index > 0)
            {
                throw new KbParserException($"Invalid syntax at [\"] at position: [{index:n0}]");
            }
        }

        private void ValidateParentheses()
        {
            int parenOpen = 0;
            int parenClose = 0;

            for (int i = 0; i < _text.Length; i++)
            {
                if (_text[i] == '(')
                {
                    parenOpen++;
                }
                else if (_text[i] == ')')
                {
                    parenClose++;
                }

                if (parenClose > parenOpen)
                {
                    throw new KbParserException($"Parentheses mismatch in expression: [{_text}]");
                }
            }

            if (parenClose != parenOpen)
            {
                throw new KbParserException($"Parentheses mismatch in expression: [{_text}]");
            }
        }
    }
}
