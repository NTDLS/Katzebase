using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarChecksum
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var text = function.Get<string>("text");
            if (text == null)
            {
                return null;
            }
            return Shared.Helpers.Checksum(text).ToString();
        }
    }
}
