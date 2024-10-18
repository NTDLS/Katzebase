using NTDLS.Katzebase.Parsers.Functions.Scalar;
namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsDouble
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var value = function.Get<string?>("value");
            if (value == null)
            {
                return null;
            }
            return (double.TryParse(value, out _) ? 1 : 0).ToString();
        }
    }
}
