using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserCreateSchema
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Create)
            {
                SubQueryType = SubQueryType.Schema
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException($"Invalid query. Found [{schemaName}], expected: schema name.");
            }
            query.Schemas.Add(new QuerySchema(schemaName));
            query.AddAttribute(PreparedQuery.QueryAttribute.Schema, schemaName);

            if (tokenizer.TryEatIfNext("with"))
            {
                var options = new ExpectedWithOptions
                {
                    {"pagesize", typeof(uint) }
                };

                query.AddAttributes(StaticParserWithOptions.Parse(tokenizer, options));
            }

            return query;
        }
    }
}
