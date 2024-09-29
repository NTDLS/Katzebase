using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIIF
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return function.Get<bool>("condition") ? function.Get<string>("whenTrue") : function.Get<string>("whenFalse");
        }
    }
}
