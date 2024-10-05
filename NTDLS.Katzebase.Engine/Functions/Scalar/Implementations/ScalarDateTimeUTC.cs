using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarDateTimeUTC
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return DateTime.UtcNow.ToString(function.Get<string>("format"));
        }
    }
}
