﻿using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Interfaces;


namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerDocumentPage
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function, KbInsensitiveDictionary<TData?> rowValues) where TData : IStringable
        {
            var rowId = rowValues.FirstOrDefault(o => o.Key == $"{function.Get<string>("schemaAlias")}.{EngineConstants.UIDMarker}");
            if (rowId.Value == null)
            {
                return null;
            }
            else
            {
                return DocumentPointer<TData>.Parse(rowId.Value.ToString()).PageNumber.ToString();
            }
        }
    }
}
