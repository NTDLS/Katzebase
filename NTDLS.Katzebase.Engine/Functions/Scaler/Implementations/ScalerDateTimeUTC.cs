namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerDateTimeUTC
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function)
        {
            return DateTime.UtcNow.ToString(function.Get<string>("format"));
        }
    }
}
