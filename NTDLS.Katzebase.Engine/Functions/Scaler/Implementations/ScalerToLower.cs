namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerToLower
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return function.Get<string>("text").ToLowerInvariant();
        }
    }
}
