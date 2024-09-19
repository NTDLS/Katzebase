using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns true if the next token is a member of the given enum.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public T EatIfNextEnum<T>(char[] delimiters, out string outFoundToken) where T : Enum
        {
            int restoreCaret = _caret;
            outFoundToken = EatGetNext(delimiters, out _);

            if (Enum.TryParse(typeof(T), outFoundToken, true, out object? parsedValue))
            {
                if (Enum.IsDefined(typeof(T), parsedValue) && int.TryParse(outFoundToken, out _) == false)
                {
                    return (T)parsedValue;
                }
            }

            _caret = restoreCaret;

            throw new KbParserException($"Invalid token, found [{outFoundToken}] expected [{string.Join("],[", Enum.GetNames(typeof(T)))}]");
        }

        public T EatIfNextEnum<T>(out string outFoundToken) where T : Enum
            => EatIfNextEnum<T>(_standardTokenDelimiters, out outFoundToken);

        public T EatIfNextEnum<T>() where T : Enum
            => EatIfNextEnum<T>(_standardTokenDelimiters, out _);
    }
}
