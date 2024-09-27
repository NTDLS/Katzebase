namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerLeft
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function)
        {
            return function.Get<string>("text").Substring(0, function.Get<int>("length"));
        }
    }
}
