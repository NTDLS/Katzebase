using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        public void SetText(string text, int caret)
        {
            _text = text;
            _caret = caret;
            if (_caret >= _text.Length)
            {
                throw new KbParserException("Caret position is greater than text length.");
            }
        }

        public void SetText(string text)
        {
            _text = text;
            if (_caret >= _text.Length)
            {
                throw new KbParserException("Caret position is greater than text length.");
            }
        }
    }
}
