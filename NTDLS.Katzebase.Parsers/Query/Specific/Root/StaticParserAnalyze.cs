using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific.Root
{
    public static class StaticParserAnalyze
    {
        internal static SupportingTypes.Query Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum(
                [
                    SubQueryType.Index,
                    SubQueryType.Schema
                ]);

            return querySubType switch
            {
                SubQueryType.Schema => StaticParserAnalyzeSchema.Parse(queryBatch, tokenizer),
                SubQueryType.Index => StaticParserAnalyzeIndex.Parse(queryBatch, tokenizer),

                _ => throw new KbNotImplementedException($"Query type is not implemented: [{querySubType}].")
            };
        }
    }
}
