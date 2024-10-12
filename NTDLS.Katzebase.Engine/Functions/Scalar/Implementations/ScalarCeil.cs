using NTDLS.Helpers;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarCeil
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var value = function.Get<double?>("value");
            if (value == null)
            {
                return null;
            }
            return Math.Ceiling(value.EnsureNotNull()).ToString();
        }
    }
}
