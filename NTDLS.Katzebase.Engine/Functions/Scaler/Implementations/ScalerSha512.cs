namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerSha512
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function)
        {
            return Library.Helpers.GetSHA512Hash(function.Get<string>("text"));
        }
    }
}
