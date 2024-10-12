using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarToNumeric
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return double.Parse(function.Get<string>("value")).ToString();
        }
    }
}
