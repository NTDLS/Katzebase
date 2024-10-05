using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarGuid
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return Guid.NewGuid().ToString();
        }
    }
}
