using Newtonsoft.Json.Linq;
using NTDLS.Helpers;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarPow
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var x = function.Get<double?>("x");
            var y = function.Get<double?>("y");
            if (x == null || y == null)
            {
                return null;
            }
            return Math.Pow(x.EnsureNotNull(), y.EnsureNotNull()).ToString();
        }
    }
}
