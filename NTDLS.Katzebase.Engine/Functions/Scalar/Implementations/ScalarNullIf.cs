using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarNullIf
    {
        //"String NullIf (String value, Boolean conditional)|'Returns null if the supplied conditional is true.'",

        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            if (function.Get<bool?>("conditional") == true)
            {
                return null;
            }
            return function.Get<string?>("value");
        }
    }
}
