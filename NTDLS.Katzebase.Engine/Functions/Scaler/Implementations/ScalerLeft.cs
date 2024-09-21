using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerLeft
    {
        public static string? Execute(Transaction transaction, ScalerFunctionParameterValueCollection function, KbInsensitiveDictionary<string?> rowFields)
        {
            return function.Get<string>("text").Substring(0, function.Get<int>("length"));
        }
    }
}
