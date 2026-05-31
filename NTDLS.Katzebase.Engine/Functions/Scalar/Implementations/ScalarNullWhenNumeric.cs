using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarNullWhenNumeric
    {
        //"Numeric NullWhenNumeric (Numeric value, Numeric compareToValue)|'Returns null if the supplied value is equal to the compareToValue.'",

        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            if (function.Get<double?>("value") == function.Get<double?>("compareToValue"))
            {
                return null;
            }
            return function.Get<string?>("value");
        }
    }
}
