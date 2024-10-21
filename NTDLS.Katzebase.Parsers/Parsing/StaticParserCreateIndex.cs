using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Parsing
{
    public static class StaticParserCreateIndex
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Create, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Index
            };

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var indexName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected index name, found: [{indexName}].");
            }

            query.AddAttribute(PreparedQuery.Attribute.IndexName, indexName);
            query.AddAttribute(PreparedQuery.Attribute.IsUnique, false);

            if (string.IsNullOrEmpty(tokenizer.MatchingScope(out var endOfScope)))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Expected index field body.");
            }

            tokenizer.EatIfNext('(');

            foreach (var field in tokenizer.EatScopeSensitiveSplit(endOfScope))
            {
                if (!field.IsIdentifier())
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected field name, found: [{tokenizer.Variables.Resolve(field)}].");
                }

                query.CreateIndexFields.Add(field);
            }

            tokenizer.EatIfNext(')');

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));
            query.AddAttribute(PreparedQuery.Attribute.Schema, schemaName);

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
