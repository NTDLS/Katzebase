using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerSha256
    {
        public static string? Execute(Transaction transaction, ScalerFunctionParameterValueCollection function, KbInsensitiveDictionary<string?> rowFields)
        {
            return Library.Helpers.GetSHA256Hash(function.Get<string>("text"));
        }
    }
}
