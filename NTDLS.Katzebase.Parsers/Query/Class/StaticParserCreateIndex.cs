using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using System.Collections.Generic;
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
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{indexName}], expected: index name.");
            }

            query.AddAttribute(PreparedQuery.QueryAttribute.IndexName, indexName);
            query.AddAttribute(PreparedQuery.QueryAttribute.IsUnique, false);

            if (string.IsNullOrEmpty(tokenizer.MatchingScope(out var endOfScope)) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Expected: index field body.");
            }

            tokenizer.EatIfNext('(');

            foreach (var field in tokenizer.EatScopeSensitiveSplit(endOfScope))
            {
                if (field.IsIdentifier())
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Found '" + field + "', expected: field name.");
                }

                query.CreateIndexFields.Add(field);
            }

            tokenizer.EatIfNext(')');

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Invalid query. Found '" + indexName + "', expected: schema name.");
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
