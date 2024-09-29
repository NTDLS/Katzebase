using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerGuid
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return Guid.NewGuid().ToString();
        }
    }
}
