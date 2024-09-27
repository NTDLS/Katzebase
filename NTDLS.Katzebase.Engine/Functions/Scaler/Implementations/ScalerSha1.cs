namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerSha1
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function)
        {
            return Library.Helpers.GetSHA1Hash(function.Get<string>("text"));
        }
    }
}
