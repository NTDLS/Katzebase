using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarNullIfNumeric
    {
        //"Numeric NullIfNumeric (Numeric value, Boolean conditional)|'Returns null if the supplied conditional is true.'",

        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            if (function.Get<bool>("conditional"))
            {
                return null;
            }
            return function.Get<string>("value");
        }
    }
}
