using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerDateTime
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return DateTime.Now.ToString(function.Get<string>("format"));
        }
    }
}
