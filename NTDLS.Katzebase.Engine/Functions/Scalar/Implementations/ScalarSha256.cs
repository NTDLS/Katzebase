using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarSha256
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return Shared.Helpers.GetSHA256Hash(function.Get<string>("text"));
        }
    }
}
