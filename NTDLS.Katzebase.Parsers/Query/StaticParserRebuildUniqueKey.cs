using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Query
{
    public static class StaticParserRebuildUniqueKey
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Rebuild, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.UniqueKey
            };

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var indexName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected unique key name, found: [{tokenizer.Variables.Resolve(indexName)}].");
            }
            query.AddAttribute(PreparedQuery.Attribute.IndexName, indexName);

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{tokenizer.Variables.Resolve(schemaName)}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));

            if (tokenizer.TryEatIfNext("with"))
            {
                var options = new ExpectedQueryAttributes
                {
                    {"partitions", typeof(uint) }
                };
                query.AddAttributes(StaticParserAttributes.Parse(tokenizer, options));
            }

            return query;
        }
    }
}
