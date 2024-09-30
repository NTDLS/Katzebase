using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {

        private void RecordBreadcrumb()
        {
            if (_breadCrumbs.Count == 0 || _breadCrumbs.Peek() != Caret)
            {
                _breadCrumbs.Push(Caret);
            }
        }

        public int? GetLineNumber(int caret)
        {
            return LineRanges.FirstOrDefault(o => caret >= o.Begin && caret <= o.End)?.Line;
        }

        public int? GetCurrentLineNumber()
        {
            return GetLineNumber(Caret);
        }

        /// <summary>
        /// Places the caret back to the beginning.
        /// </summary>
        public void Rewind()
        {
            Caret = 0;
            _breadCrumbs.Clear();
        }

        /// <summary>
        /// Returns the position of the caret before the previous tokenization operation.
        /// </summary>
        /// <returns></returns>
        public int PreviousCaret()
        {
            if (_breadCrumbs.Count == 0)
            {
                throw new KbParserException(GetCurrentLineNumber(), "Tokenization steps are out of range.");
            }

            return _breadCrumbs.Peek();
        }

        /// <summary>
        /// Sets the caret to where it was before the previous tokenization operation.
        /// </summary>
        /// <returns></returns>
        public void StepBack()
        {
            if (_breadCrumbs.Count == 0)
            {
                throw new KbParserException(GetCurrentLineNumber(), "Tokenization steps are out of range.");
            }
            Caret = _breadCrumbs.Pop();
            throw new KbParserException(GetCurrentLineNumber(), "Tokenization steps are out of range.");
        }

        /// <summary>
        /// Sets the caret to where it was before the previous n-tokenization operations.
        /// </summary>
        /// <returns></returns>
        public void StepBack(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_breadCrumbs.Count == 0)
                {
                    throw new KbParserException(GetCurrentLineNumber(), "Tokenization steps are out of range.");
                }
                Caret = _breadCrumbs.Pop();
            }
        }

        public void SetCaret(int position)
        {
            if (position > _text.Length)
            {
                throw new KbParserException(GetCurrentLineNumber(), "Tokenization caret moved past end of text.");
            }
            else if (position < 0)
            {
                throw new KbParserException(GetCurrentLineNumber(), "Tokenization caret moved past beginning of text.");
            }
            Caret = position;
        }

        /// <summary>
        /// Skips the next character in the sequence.
        /// </summary>
        /// <exception cref="KbParserException"></exception>
        public char EatNextCharacter()
        {
            RecordBreadcrumb();

            int index = Caret;

            if (Caret >= _text.Length)
            {
                throw new KbParserException(GetCurrentLineNumber(), "The tokenizer sequence is empty.");
            }

            Caret++;
            InternalEatWhiteSpace();

            return _text[index];
        }
    }
}
