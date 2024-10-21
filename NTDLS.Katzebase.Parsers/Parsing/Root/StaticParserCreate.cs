using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Parsing;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Parsing.Root
{
    public static class StaticParserCreate
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
                SubQueryType.Schema => StaticParserCreateSchema.Parse(queryBatch, tokenizer),
                SubQueryType.Index => StaticParserCreateIndex.Parse(queryBatch, tokenizer),
                SubQueryType.UniqueKey => StaticParserCreateUniqueKey.Parse(queryBatch, tokenizer),
                SubQueryType.Procedure => StaticParserCreateProcedure.Parse(queryBatch, tokenizer),
                SubQueryType.Role => StaticParserCreateRole.Parse(queryBatch, tokenizer),
                SubQueryType.Account => StaticParserCreateAccount.Parse(queryBatch, tokenizer),
                _ => throw new KbNotImplementedException($"Query type is not implemented: [{querySubType}].")
            };
        }
    }
}
