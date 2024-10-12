using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsNull
    {
        //"Boolean IsNull (String value)|'Returns true if the given value is null, otherwise false.'",

        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return (function.Get<string?>("value") == null).ToString();
        }
    }
}
