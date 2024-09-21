using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerChecksum
    {
        public static string? Execute(Transaction transaction, ScalerFunctionParameterValueCollection function, KbInsensitiveDictionary<string?> rowFields)
        {
            return Library.Helpers.Checksum(function.Get<string>("text")).ToString();
        }
    }
}
