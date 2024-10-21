using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Root
{
    public static class StaticParserList
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum(
                [
                    SubQueryType.Documents,
                    SubQueryType.Schemas
                ]);

            return querySubType switch
            {
                SubQueryType.Documents => StaticParserListDocuments.Parse(queryBatch, tokenizer),
                SubQueryType.Schemas => StaticParserListSchemas.Parse(queryBatch, tokenizer),

                _ => throw new KbNotImplementedException($"Query type is not implemented: [{querySubType}].")
            };
        }
    }
}
