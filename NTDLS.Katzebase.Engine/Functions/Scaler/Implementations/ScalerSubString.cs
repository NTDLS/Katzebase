namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerSubString
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            return function.Get<string>("text").Substring(function.Get<int>("startIndex"), function.Get<int>("length"));
        }
    }
}
