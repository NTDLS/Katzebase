using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns true if the next token is a member of the given enum.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        /// <typeparam name="T">The enum type to parse values for.</typeparam>
        /// <param name="delimiters">Delimiters to use when parsing tokens.</param>
        /// <param name="outFoundToken">The enum value found.</param>
        /// <param name="validValues">Optional array of acceptable enum values to allow.</param>
        /// <returns></returns>
        /// <exception cref="KbParserException"></exception>
        public T EatIfNextEnum<T>(char[] delimiters, out string outFoundToken, T[]? validValues = null) where T : Enum
        {
            int restoreCaret = _caret;
            outFoundToken = EatGetNext(delimiters, out _);

            if (Enum.TryParse(typeof(T), outFoundToken, true, out object? parsedValue))
            {
                if (Enum.IsDefined(typeof(T), parsedValue) && int.TryParse(outFoundToken, out _) == false)
                {
                    if (validValues != null)
                    {
                        if (!validValues.Contains((T)parsedValue))
                        {
                            throw new KbParserException($"Invalid token, found [{outFoundToken}] expected [{string.Join("],[", validValues)}]");
                        }

                        return (T)parsedValue;
                    }
                    return (T)parsedValue;

                }
            }

            _caret = restoreCaret;

            throw new KbParserException($"Invalid token, found [{outFoundToken}] expected [{string.Join("],[", Enum.GetNames(typeof(T)))}]");
        }

        public T EatIfNextEnum<T>(out string outFoundToken) where T : Enum
            => EatIfNextEnum<T>(_standardTokenDelimiters, out outFoundToken);

        public T EatIfNextEnum<T>(T[] validValues) where T : Enum
            => EatIfNextEnum<T>(_standardTokenDelimiters, out _, validValues);

        public T EatIfNextEnum<T>() where T : Enum
            => EatIfNextEnum<T>(_standardTokenDelimiters, out _);
    }
}
