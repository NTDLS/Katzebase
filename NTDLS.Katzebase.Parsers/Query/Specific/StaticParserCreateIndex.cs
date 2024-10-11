using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.Query.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserCreateIndex
    {
        internal static SupportingTypes.Query Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new SupportingTypes.Query(queryBatch, QueryType.Create, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Index
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var indexName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected index name, found: [{indexName}].");
            }

            query.AddAttribute(SupportingTypes.Query.Attribute.IndexName, indexName);
            query.AddAttribute(SupportingTypes.Query.Attribute.IsUnique, false);

            if (string.IsNullOrEmpty(tokenizer.MatchingScope(out var endOfScope)))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Expected index field body.");
            }

            tokenizer.EatIfNext('(');

            foreach (var field in tokenizer.EatScopeSensitiveSplit(endOfScope))
            {
                if (!field.IsIdentifier())
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected field name, found: [{tokenizer.ResolveLiteral(field)}].");
                }

                query.CreateIndexFields.Add(field);
            }

            tokenizer.EatIfNext(')');

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));
            query.AddAttribute(SupportingTypes.Query.Attribute.Schema, schemaName);

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
