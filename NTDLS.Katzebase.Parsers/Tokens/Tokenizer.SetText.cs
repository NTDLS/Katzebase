using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        public void SetText(string text, int caret)
        {
            _text = text;
            Caret = caret;
            if (Caret >= _length)
            {
                throw new KbParserException(GetCurrentLineNumber(), "Caret position is greater than text length.");
            }
        }

        public void SetText(string text)
        {
            _text = text;
            if (Caret >= _length)
            {
                throw new KbParserException(GetCurrentLineNumber(), "Caret position is greater than text length.");
            }
        }
    }
}
