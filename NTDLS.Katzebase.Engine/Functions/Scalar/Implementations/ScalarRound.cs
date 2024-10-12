using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarRound
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var value = function.Get<decimal>("value");
            var decimalPlaces = function.Get<int>("decimalPlaces");
            return Math.Round(value, decimalPlaces).ToString();
        }
    }
}
