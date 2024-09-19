using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        private void RecordBreadcrumb()
        {
            if (_breadCrumbs.Count == 0 || _breadCrumbs.Peek() != _caret)
            {
                _breadCrumbs.Push(_caret);
            }
        }

        /// <summary>
        /// Places the caret back to the beginning.
        /// </summary>
        public void Rewind()
        {
            _caret = 0;
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
                throw new KbParserException("Tokenization steps are out of range.");
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
                throw new KbParserException("Tokenization steps are out of range.");
            }
            _caret = _breadCrumbs.Pop();
            throw new KbParserException("Tokenization steps are out of range.");
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
                    throw new KbParserException("Tokenization steps are out of range.");
                }
                _caret = _breadCrumbs.Pop();
            }
        }

        public void SetCaret(int position)
        {
            if (position > _text.Length)
            {
                throw new KbParserException("Tokenization caret moved past end of text.");
            }
            else if (position < 0)
            {
                throw new KbParserException("Tokenization caret moved past beginning of text.");
            }
            _caret = position;
        }

        /// <summary>
        /// Skips the next character in the sequence.
        /// </summary>
        /// <exception cref="KbParserException"></exception>
        public char EatNextCharacter()
        {
            RecordBreadcrumb();

            int index = _caret;

            if (_caret >= _text.Length)
            {
                throw new KbParserException("The tokenizer sequence is empty.");
            }

            _caret++;
            InternalEatWhiteSpace();

            return _text[index];
        }
    }
}
