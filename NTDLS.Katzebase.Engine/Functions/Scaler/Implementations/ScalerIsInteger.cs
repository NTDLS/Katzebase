using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIsInteger
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return (int.TryParse(function.Get<string>("value"), out _) == false).ToString();
        }
    }
}
