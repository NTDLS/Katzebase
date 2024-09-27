namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerSubString
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function)
        {
            return function.Get<string>("text").Substring(function.Get<int>("startIndex"), function.Get<int>("length"));
        }
    }
}
