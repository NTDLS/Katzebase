using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIsString
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return (double.TryParse(function.Get<string>("value"), out _) == false).ToString();
        }
    }
}
