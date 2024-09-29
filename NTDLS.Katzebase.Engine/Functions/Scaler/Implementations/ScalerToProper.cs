using NTDLS.Katzebase.Parsers.Functions.Scaler;
using System.Globalization;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerToProper
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(function.Get<string>("text"));
        }
    }
}
