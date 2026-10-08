using NTDLS.Katzebase.Parsers.Functions.Scalar;
using System.Globalization;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsInteger
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var value = function.Get<string?>("value");
            if (value == null)
            {
                return null;
            }
            return (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? 1 : 0).ToString();
        }
    }
}
