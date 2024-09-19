using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        public bool TryEatIfNextEnum<T>(char[] delimiters, out string outFoundToken, [NotNullWhen(true)] out T? value) where T : Enum
        {
            outFoundToken = string.Empty;
            try
            {
                value = EatIfNextEnum<T>(delimiters, out outFoundToken);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public bool TryEatIfNextEnum<T>([NotNullWhen(true)] out T? value) where T : Enum
            => TryEatIfNextEnum(_standardTokenDelimiters, out _, out value);
    }
}
