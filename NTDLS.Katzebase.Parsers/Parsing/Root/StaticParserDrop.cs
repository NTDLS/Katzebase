using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Parsing.Root
{
    public static class StaticParserDrop
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum(
                [
                    SubQueryType.Account,
                    SubQueryType.Index,
                    SubQueryType.Procedure,
                    SubQueryType.Role,
                    SubQueryType.Schema,
                    SubQueryType.UniqueKey
                ]);

            return querySubType switch
            {
                SubQueryType.Schema => StaticParserDropSchema.Parse(queryBatch, tokenizer),
                SubQueryType.Index => StaticParserDropIndex.Parse(queryBatch, tokenizer),
                SubQueryType.UniqueKey => StaticParserDropUniqueKey.Parse(queryBatch, tokenizer),
                SubQueryType.Procedure => StaticParserDropProcedure.Parse(queryBatch, tokenizer),
                SubQueryType.Account => StaticParserDropAccount.Parse(queryBatch, tokenizer),
                SubQueryType.Role => StaticParserDropRole.Parse(queryBatch, tokenizer),

                _ => throw new KbNotImplementedException($"Query type is not implemented: [{querySubType}].")
            };
        }
    }
}
