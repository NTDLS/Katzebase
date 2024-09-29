using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerDocumentPage
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function, KbInsensitiveDictionary<string?> rowValues)
        {
            var rowId = rowValues.FirstOrDefault(o => o.Key == $"{function.Get<string>("schemaAlias")}.{EngineConstants.UIDMarker}");
            if (rowId.Value == null)
            {
                return null;
            }
            return DocumentPointer.Parse(rowId.Value).PageNumber.ToString();
        }
    }
}
