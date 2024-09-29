using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerLastIndexOf
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
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
