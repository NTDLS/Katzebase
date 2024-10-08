using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserRebuild
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Index, SubQueryType.UniqueKey]);

            return querySubType switch
            {
                SubQueryType.Index => StaticParserRebuildIndex.Parse(queryBatch, tokenizer),
                SubQueryType.UniqueKey => StaticParserRebuildUniqueKey.Parse(queryBatch, tokenizer),

                _ => throw new KbNotImplementedException($"Query type is not implemented: [{querySubType}].")
            };
        }
    }
}

