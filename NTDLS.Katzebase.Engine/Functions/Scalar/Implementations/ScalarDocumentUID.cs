using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarDocumentUID
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function, KbInsensitiveDictionary<string?> rowValues)
        {
            var rowId = rowValues.FirstOrDefault(o => o.Key == $"{function.Get<string>("schemaAlias")}.{EngineConstants.UIDMarker}");
            return rowId.Value;
        }
    }
}
