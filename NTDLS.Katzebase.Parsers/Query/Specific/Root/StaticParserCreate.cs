using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific.Root
{
    public static class StaticParserCreate
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Schema, SubQueryType.Index,
                SubQueryType.Role, SubQueryType.Account, SubQueryType.UniqueKey, SubQueryType.Procedure]);

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
