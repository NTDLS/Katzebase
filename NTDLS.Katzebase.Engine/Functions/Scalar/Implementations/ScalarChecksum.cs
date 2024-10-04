using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarChecksum
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return Shared.Helpers.Checksum(function.Get<string>("text")).ToString();
        }
    }
}
