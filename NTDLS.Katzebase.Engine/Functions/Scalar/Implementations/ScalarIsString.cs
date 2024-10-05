using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsString
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return (double.TryParse(function.Get<string>("value"), out _) == false).ToString();
        }
    }
}
