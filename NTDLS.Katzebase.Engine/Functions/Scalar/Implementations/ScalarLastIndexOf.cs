using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarLastIndexOf
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            int startIndex = function.Get<int>("offset");
            if (startIndex > 0)
            {
                return function.Get<string>("textToSearch").LastIndexOf(function.Get<string>("textToFind"), startIndex, StringComparison.InvariantCultureIgnoreCase).ToString();
            }
            return function.Get<string>("textToSearch").LastIndexOf(function.Get<string>("textToFind"), StringComparison.InvariantCultureIgnoreCase).ToString();
        }
    }
}
