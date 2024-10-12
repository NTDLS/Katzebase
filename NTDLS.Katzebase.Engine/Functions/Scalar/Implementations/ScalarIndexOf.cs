using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIndexOf
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var textToSearch = function.Get<string>("textToSearch");
            var textToFind = function.Get<string>("textToFind");
            if (textToSearch == null || textToFind == null)
            {
                return null;
            }
            return textToSearch.IndexOf(textToFind, function.Get<int>("offset"), StringComparison.InvariantCultureIgnoreCase).ToString();
        }
    }
}
