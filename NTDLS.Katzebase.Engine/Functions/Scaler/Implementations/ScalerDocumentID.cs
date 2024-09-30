using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerDocumentID
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function, KbInsensitiveDictionary<string?> rowValues)
        {
            var rowId = rowValues.FirstOrDefault(o => o.Key == $"{function.Get<string>("schemaAlias")}.{EngineConstants.UIDMarker}");
            if (rowId.Value == null)
            {
                return null;
            }
            return DocumentPointer.Parse(rowId.Value).DocumentId.ToString();
        }
    }
}
