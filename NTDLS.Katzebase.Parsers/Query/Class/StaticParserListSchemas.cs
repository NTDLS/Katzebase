using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserListSchemas
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.List)
            {
                SubQueryType = SubQueryType.Schemas
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{schemaName}], expected: schema name.");
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
