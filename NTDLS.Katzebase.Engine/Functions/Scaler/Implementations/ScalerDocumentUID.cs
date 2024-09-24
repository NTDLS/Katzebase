using fs;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Library;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerDocumentUID
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function, KbInsensitiveDictionary<fstring?> rowValues)
        {
            var rowId = rowValues.FirstOrDefault(o => o.Key == $"{function.Get<string>("schemaAlias")}.{EngineConstants.UIDMarker}");
            return rowId.Value.s;
        }
    }
}
