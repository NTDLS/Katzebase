using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerDateTimeUTC
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return DateTime.UtcNow.ToString(function.Get<string>("format"));
        }
    }
}
