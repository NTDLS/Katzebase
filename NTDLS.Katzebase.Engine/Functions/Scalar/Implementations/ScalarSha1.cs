using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarSha1
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return Shared.Helpers.GetSHA1Hash(function.Get<string>("text"));
        }
    }
}
