using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserCreateIndex
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Create)
            {
                SubQueryType = SubQueryType.Index
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var indexName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected index name, found: [{indexName}].");
            }

            query.AddAttribute(PreparedQuery.QueryAttribute.IndexName, indexName);
            query.AddAttribute(PreparedQuery.QueryAttribute.IsUnique, false);

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
            query.Schemas.Add(new QuerySchema(schemaName));
            query.AddAttribute(PreparedQuery.QueryAttribute.Schema, schemaName);

            if (tokenizer.TryEatIfNext("with"))
            {
                var options = new ExpectedWithOptions
                {
                    {"partitions", typeof(uint) }
                };

                query.AddAttributes(StaticParserWithOptions.Parse(tokenizer, options));
            }

            return query;
        }
    }
}
