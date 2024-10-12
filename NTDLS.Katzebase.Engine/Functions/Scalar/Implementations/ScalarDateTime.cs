using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarDateTime
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return DateTime.Now.ToString(function.Get<string?>("format"));
        }
    }
}
