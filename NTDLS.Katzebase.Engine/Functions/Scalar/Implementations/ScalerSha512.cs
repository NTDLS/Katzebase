using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerSha512
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            return Library.Helpers.GetSHA512Hash(function.Get<string>("text"));
        }
    }
}
