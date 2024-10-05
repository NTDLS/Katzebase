using NTDLS.Katzebase.Parsers.Functions.Scalar;
using System.Globalization;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarToProper
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(function.Get<string>("text"));
        }
    }
}
