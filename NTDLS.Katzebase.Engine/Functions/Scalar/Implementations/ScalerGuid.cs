using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerGuid
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            return Guid.NewGuid().ToString();
        }
    }
}
