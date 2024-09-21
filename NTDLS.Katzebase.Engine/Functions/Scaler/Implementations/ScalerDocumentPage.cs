using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Library;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerDocumentPage
    {
        public static string? Execute(Transaction transaction, ScalerFunctionParameterValueCollection function, KbInsensitiveDictionary<string?> rowFields)
        {
            var rowId = rowFields.FirstOrDefault(o => o.Key == $"{function.Get<string>("schemaAlias")}.{EngineConstants.UIDMarker}");
            if (rowId.Value == null)
            {
                return null;
            }
            return DocumentPointer.Parse(rowId.Value).PageNumber.ToString();
        }
    }
}
