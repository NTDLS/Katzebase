using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerIsInteger
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            return (int.TryParse(function.Get<string>("value"), out _) == false).ToString();
        }
    }
}
