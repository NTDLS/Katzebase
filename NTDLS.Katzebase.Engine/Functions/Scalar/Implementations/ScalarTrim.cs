using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarTrim
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var characters = function.GetNullable<string?>("characters");
            if (characters != null)
            {
                return function.Get<string>("text")?.Trim(characters.ToCharArray());
            }

            return function.Get<string>("text")?.Trim();
        }
    }
}
