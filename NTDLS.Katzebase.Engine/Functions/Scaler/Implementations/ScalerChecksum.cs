namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerChecksum
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function)
        {
            return Library.Helpers.Checksum(function.Get<string>("text")).ToString();
        }
    }
}
