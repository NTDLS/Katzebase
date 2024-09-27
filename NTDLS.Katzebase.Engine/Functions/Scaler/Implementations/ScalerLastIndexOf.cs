namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerLastIndexOf
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            return function.Get<string>("textToSearch").LastIndexOf(function.Get<string>("textToFind")).ToString();
        }
    }
}
