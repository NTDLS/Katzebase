using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Parsing;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Parsing.Root
{
    public static class StaticParserAlter
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum(
                [
                    SubQueryType.Configuration,
                    SubQueryType.Role,
                    SubQueryType.Schema
                ]);

            return querySubType switch
            {
                SubQueryType.Schema => StaticParserAlterSchema.Parse(queryBatch, tokenizer),
                SubQueryType.Configuration => StaticParserAlterConfiguration.Parse(queryBatch, tokenizer),
                SubQueryType.Role => StaticParserAlterRole.Parse(queryBatch, tokenizer),

                _ => throw new KbNotImplementedException($"The query type is not implemented: [{querySubType}].")
            };
        }
    }
}
