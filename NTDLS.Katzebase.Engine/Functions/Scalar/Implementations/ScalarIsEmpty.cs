using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsEmpty
    {
        //"Boolean IsEmpty (String value = null)|'Returns true if the given value is null or empty.'"
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return string.IsNullOrEmpty(function.Get<string?>("value")) ? "true" : "false";
        }
    }
}
