using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        public bool TryEatIsNextEnum<T>(char[] delimiters, out string outFoundToken, [NotNullWhen(true)] out T? value) where T : Enum
        {
            outFoundToken = string.Empty;
            try
            {
                value = EatIsNextEnumToken<T>(delimiters, out outFoundToken);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public bool TryEatIsNextEnumToken<T>([NotNullWhen(true)] out T? value) where T : Enum
            => TryEatIsNextEnum(_standardTokenDelimiters, out _, out value);
    }
}
