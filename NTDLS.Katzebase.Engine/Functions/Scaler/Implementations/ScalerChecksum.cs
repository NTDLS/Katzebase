using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerChecksum
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return Shared.Helpers.Checksum(function.Get<string>("text")).ToString();
        }
    }
}
