using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIfNullNumeric
    {
        //"Numeric IfNullNumeric (Numeric value, Numeric defaultValue)|'Returns the supplied default value when the given value is null.'",

        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var value = function.Get<string?>("value");
            if (value == null)
            {
                return function.Get<string?>("defaultValue");
            }
            return value;
        }
    }
}
