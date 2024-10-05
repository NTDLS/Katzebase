using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsInteger
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return (int.TryParse(function.Get<string>("value"), out _) == false).ToString();
        }
    }
}
