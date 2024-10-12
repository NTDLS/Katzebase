using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarSha256
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var text = function.Get<string?>("text");
            if (text == null)
            {
                return null;
            }
            return Shared.Helpers.GetSHA256Hash(text);
        }
    }
}
