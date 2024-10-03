using NTDLS.Katzebase.Parsers.Interfaces;

using NTDLS.Katzebase.Parsers.Functions.Scaler;
namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerDateTimeUTC
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            return DateTime.UtcNow.ToString(function.Get<string>("format"));
        }
    }
}
