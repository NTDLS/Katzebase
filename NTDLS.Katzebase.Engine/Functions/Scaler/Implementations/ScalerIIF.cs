namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIIF
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function)
        {
            return function.Get<bool>("condition") ? function.Get<string>("whenTrue") : function.Get<string>("whenFalse");
        }
    }
}
