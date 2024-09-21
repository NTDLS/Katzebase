using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserRebuild
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Index, SubQueryType.UniqueKey]);

            return querySubType switch
            {
                SubQueryType.Index => StaticParserRebuildIndex.Parse(queryBatch, tokenizer),
                SubQueryType.UniqueKey => StaticParserRebuildUniqueKey.Parse(queryBatch, tokenizer),

                _ => throw new KbParserException($"The query type is not implemented: [{querySubType}].")
            };
        }
    }
}

