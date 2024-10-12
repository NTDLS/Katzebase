using NTDLS.Helpers;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarRound
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var value = function.Get<decimal?>("value");
            var decimalPlaces = function.Get<int?>("decimalPlaces");

            if (value == null || decimalPlaces == null)
            {
                return null;
            }

            return Math.Round(value.EnsureNotNull(), decimalPlaces.EnsureNotNull()).ToString();
        }
    }
}
