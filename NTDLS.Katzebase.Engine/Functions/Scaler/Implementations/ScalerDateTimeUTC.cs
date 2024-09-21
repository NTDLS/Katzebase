using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerDateTimeUTC
    {
        public static string? Execute(Transaction transaction, ScalerFunctionParameterValueCollection function, KbInsensitiveDictionary<string?> rowFields)
        {
            return DateTime.UtcNow.ToString(function.Get<string>("format"));
        }
    }
}
