using NTDLS.Katzebase.Parsers.Functions.Scalar;
using System.Globalization;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarToProper
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var text = function.Get<string>("text");
            if (text == null)
            {
                return null;
            }
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }
    }
}
