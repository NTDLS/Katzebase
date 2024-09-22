namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerTrim
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return function.Get<string>("text").Trim();
        }
    }
}
