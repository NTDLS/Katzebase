using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIndexOf
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return function.Get<string>("textToSearch").IndexOf(
                function.Get<string>("textToFind"), function.Get<int>("offset"), StringComparison.InvariantCultureIgnoreCase).ToString();
        }
    }
}
