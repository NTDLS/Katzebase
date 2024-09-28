using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserRebuildUniqueKey<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            var query = new PreparedQuery<TData>(queryBatch, QueryType.Rebuild)
            {
                SubQueryType = SubQueryType.UniqueKey
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var indexName) == false)
            {
                throw new KbParserException($"Invalid query. Found [{indexName}], expected: unique key name.");
            }
            query.AddAttribute(PreparedQuery<TData>.QueryAttribute.IndexName, indexName);

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException($"Invalid query. Found [{schemaName}], expected: schema name.");
            }
            query.Schemas.Add(new QuerySchema<TData>(schemaName.ToLowerInvariant()));

            if (tokenizer.TryEatIfNext("with"))
            {
                var options = new ExpectedWithOptions
                {
                    {"partitions", typeof(uint) }
                };
                query.AddAttributes(StaticParserWithOptions<TData>.Parse(tokenizer, options));
            }

            return query;
        }
    }
}
