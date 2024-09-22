namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIndexOf
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return function.Get<string>("textToSearch").IndexOf(function.Get<string>("textToFind")).ToString();
        }
    }
}
