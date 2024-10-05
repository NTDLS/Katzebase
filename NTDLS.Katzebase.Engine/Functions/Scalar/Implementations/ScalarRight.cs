using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarRight
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return function.Get<string>("text").Substring(function.Get<string>("text").Length - function.Get<int>("length"));
        }
    }
}
