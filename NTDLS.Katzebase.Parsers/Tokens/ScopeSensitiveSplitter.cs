using System.Collections;
using System.Text;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    /// <summary>
    /// Allows for enumeration, stopping at a given delimiter while respecting scope. This means that we can split on a
    /// comma, but only if that comma is outside of the scope characters denoted by the supplied open and close characters.
    /// </summary>
    public class ScopeSensitiveSplitter : IEnumerable<string>
    {
        private readonly Tokenizer _tokenizer;
        private readonly int _endAtCaret;
        private readonly char _splitOn;
        private readonly char _open;
        private readonly char _close;

        public ScopeSensitiveSplitter(Tokenizer tokenizer, char splitOn, char open, char close, int endAtCaret)
        {
            _tokenizer = tokenizer;
            _splitOn = splitOn;
            _open = open;
            _close = close;
            _endAtCaret = endAtCaret;
        }

        public ScopeSensitiveSplitter(Tokenizer tokenizer, char splitOn, int endAtCaret)
        {
            _tokenizer = tokenizer;
            _splitOn = splitOn;
            _open = '(';
            _close = ')';
            _endAtCaret = endAtCaret;
        }

        public ScopeSensitiveSplitter(Tokenizer tokenizer, int endAtCaret)
        {
            _tokenizer = tokenizer;
            _splitOn = ',';
            _open = '(';
            _close = ')';
            _endAtCaret = endAtCaret;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return SplitOnDelimiter();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerator<string> SplitOnDelimiter()
        {
            int scope = 0;
            var buffer = new StringBuilder();

            int caret = _tokenizer.Caret;

            while (caret < _endAtCaret)
            {
                if (_tokenizer.Text[caret] == _open)
                {
                    scope++;
                }
                else if (_tokenizer.Text[caret] == _close)
                {
                    scope--;
                }

                if (scope == 0 && _tokenizer.Text[caret] == _splitOn)
                {
                    _tokenizer.EatWhiteSpace();
                    yield return buffer.ToString().Trim();
                    _tokenizer.Caret = caret;
                    buffer.Clear();
                }
                else
                {
                    buffer.Append(_tokenizer.Text[caret]);
                }

                caret++;
            }

            if (buffer.Length > 0)
            {
                _tokenizer.EatWhiteSpace();
                yield return buffer.ToString().Trim();
            }

            _tokenizer.Caret = caret;
            _tokenizer.EatWhiteSpace();
        }
    }
}