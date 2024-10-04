using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerIndexOf
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            return function.Get<string>("textToSearch").IndexOf(function.Get<string>("textToFind")).ToString();
        }
    }
}
