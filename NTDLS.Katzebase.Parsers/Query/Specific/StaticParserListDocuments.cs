using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.Query.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserListDocuments
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.List, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Documents
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));

            if (tokenizer.TryEatIfNext("size"))
            {
                query.RowLimit = tokenizer.EatGetNextResolved<int>();
            }
            else
            {
                query.RowLimit = 100;
            }

            return query;
        }
    }
}
