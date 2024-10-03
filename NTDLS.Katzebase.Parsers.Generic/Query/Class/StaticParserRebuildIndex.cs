using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserRebuildIndex<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            var query = new PreparedQuery<TData>(queryBatch, QueryType.Rebuild)
            {
                SubQueryType = SubQueryType.Index
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var indexName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{indexName}], expected: index name.");
            }

            query.AddAttribute(PreparedQuery<TData>.QueryAttribute.IndexName, indexName);

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{schemaName}], expected: schema name.");
            }
            query.Schemas.Add(new QuerySchema<TData>(schemaName.ToLowerInvariant()));

            if (tokenizer.TryEatIfNext("with"))
            {
                var options = new ExpectedWithOptions<TData>
                {
                    {"partitions", typeof(uint) }
                };
                query.AddAttributes(StaticParserWithOptions.Parse<TData>(tokenizer, options));
            }

            return query;
        }
    }
}
