namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIsDouble
    {
        public static string? Execute<TData>( ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            return double.TryParse(function.Get<string>("value"), out _).ToString();
        }
    }
}
