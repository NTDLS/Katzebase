using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.Query.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserRebuildUniqueKey
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Rebuild, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.UniqueKey
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var indexName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected unique key name, found: [{tokenizer.ResolveLiteral(indexName)}].");
            }
            query.AddAttribute(PreparedQuery.QueryAttribute.IndexName, indexName);

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{tokenizer.ResolveLiteral(schemaName)}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName.ToLowerInvariant(), QuerySchemaUsageType.Primary));

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
