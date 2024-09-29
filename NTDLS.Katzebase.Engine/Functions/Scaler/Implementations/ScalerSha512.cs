using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerSha512
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return Library.Helpers.GetSHA512Hash(function.Get<string>("text"));
        }
    }
}
