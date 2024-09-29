using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerSha256
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return Library.Helpers.GetSHA256Hash(function.Get<string>("text"));
        }
    }
}
