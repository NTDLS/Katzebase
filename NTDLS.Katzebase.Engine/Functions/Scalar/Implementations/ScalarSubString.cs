using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarSubString
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return function.Get<string>("text").Substring(function.Get<int>("startIndex"), function.Get<int>("length"));
        }
    }
}
