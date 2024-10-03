using NTDLS.Katzebase.Parsers.Interfaces;

using NTDLS.Katzebase.Parsers.Functions.Scaler;
namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerChecksum
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            return Library.Helpers.Checksum(function.Get<string>("text")).ToString();
        }
    }
}
