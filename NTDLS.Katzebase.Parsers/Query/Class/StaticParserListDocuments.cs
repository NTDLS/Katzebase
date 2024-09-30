using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserListDocuments
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.List)
            {
                SubQueryType = SubQueryType.Documents
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(schemaName.ToLowerInvariant()));

            if (tokenizer.TryEatIfNext("size"))
            {
                query.RowLimit = tokenizer.EatGetNextEvaluated<int>();
            }
            else
            {
                query.RowLimit = 100;
            }

            return query;
        }
    }
}
