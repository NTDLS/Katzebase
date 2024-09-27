namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIndexOf
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function)
        {
            return function.Get<string>("textToSearch").IndexOf(function.Get<string>("textToFind")).ToString();
        }
    }
}
