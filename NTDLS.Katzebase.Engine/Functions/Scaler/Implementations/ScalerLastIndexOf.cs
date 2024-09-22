namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerLastIndexOf
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return function.Get<string>("textToSearch").LastIndexOf(function.Get<string>("textToFind")).ToString();
        }
    }
}
