using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerCoalesce
    {
        public static string? Execute(Transaction transaction, ScalerFunctionParameterValueCollection function, KbInsensitiveDictionary<string?> rowFields)
        {
            foreach (var p in function.Values)
            {
                if (p != null)
                {
                    return p.Value;
                }
            }
            return null;
        }
    }
}
