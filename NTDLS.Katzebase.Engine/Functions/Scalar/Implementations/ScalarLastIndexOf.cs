using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarLastIndexOf
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var textToSearch = function.Get<string>("textToSearch");
            var textToFind = function.Get<string>("textToFind");
            if (textToSearch == null || textToFind == null)
            {
                return null;
            }

            int startIndex = function.Get<int>("offset");
            if (startIndex > 0)
            {
                return textToSearch.LastIndexOf(textToFind, startIndex, StringComparison.InvariantCultureIgnoreCase).ToString();
            }
            return textToSearch.LastIndexOf(textToFind, StringComparison.InvariantCultureIgnoreCase).ToString();
        }
    }
}
