using NTDLS.Katzebase.Parsers.Interfaces;
using System.Text;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers
{
    public class NullPropagationString(ITransaction transaction)
    {
        private readonly StringBuilder _buffer = new();

        public bool ContainsNullValue { get; private set; }

        public bool HasValueBeenSet { get; private set; }

        public int Length
        {
            get => _buffer.Length;
            set => _buffer.Length = value;
        }

        public void Append(char? c)
        {
            if (HasValueBeenSet && ContainsNullValue)
            {
                //If we already have data in the buffer, then add a NullValuePropagation warning.
                transaction.AddWarning(KbTransactionWarning.NullValuePropagation);
            }

            if (c == null)
            {
                ContainsNullValue = true;
            }

            HasValueBeenSet = true;

            _buffer.Append(c);
        }

        public void Append(string? s)
        {
            if (HasValueBeenSet && ContainsNullValue)
            {
                //If we already have data in the buffer, then add a NullValuePropagation warning.
                transaction.AddWarning(KbTransactionWarning.NullValuePropagation);
            }

            if (s == null)
            {
                ContainsNullValue = true;
            }

            HasValueBeenSet = true;

            _buffer.Append(s);
        }

        public void Clear()
        {
            HasValueBeenSet = false;
            _buffer.Clear();
        }

        public override string? ToString()
        {
            return _buffer.ToString();
        }

        public char this[Index index]
        {
            get
            {
                int actualIndex = index.IsFromEnd ? _buffer.Length - index.Value : index.Value;
                if (actualIndex < 0 || actualIndex >= _buffer.Length)
                {
                    throw new IndexOutOfRangeException();
                }

                return _buffer[actualIndex];
            }
        }

        public string? RealizedValue()
        {
            if (ContainsNullValue)
            {
                return null;
            }
            return _buffer.ToString();
        }
    }
}
