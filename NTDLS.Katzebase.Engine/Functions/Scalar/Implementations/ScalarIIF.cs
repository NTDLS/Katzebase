using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIIF
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return function.Get<bool?>("condition") == true
                ? function.Get<string?>("whenTrue")
                : function.Get<string?>("whenFalse");
        }
    }
}
