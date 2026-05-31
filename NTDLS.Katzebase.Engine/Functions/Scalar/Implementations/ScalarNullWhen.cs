using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarNullWhen
    {
        //"String NullWhen (String value, String compareToValue)|'Returns null if the supplied value is equal to the compareToValue.'",

        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            if (function.Get<string?>("value") == function.Get<string?>("compareToValue"))
            {
                return null;
            }
            return function.Get<string?>("value");
        }
    }
}
