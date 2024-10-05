using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarLength
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return function.Get<string>("text").Length.ToString();
        }
    }
}
