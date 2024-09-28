using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserDropUniqueKey<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            var query = new PreparedQuery<TData>(queryBatch, QueryType.Drop)
            {
                SubQueryType = SubQueryType.Index
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var indexName) == false)
            {
                throw new KbParserException($"Invalid query. Found [{indexName}], expected: index name.");
            }

            query.AddAttribute(PreparedQuery<TData>.QueryAttribute.IndexName, indexName);
            query.AddAttribute(PreparedQuery<TData>.QueryAttribute.IsUnique, true);

            tokenizer.IsNext('(');

            var indexFields = tokenizer.EatGetMatchingScope().Split(',').Select(o => o.Trim()).ToList();
            query.CreateIndexFields.AddRange(indexFields);

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException($"Invalid query. Found [{schemaName}], expected: schema name.");
            }
            query.Schemas.Add(new QuerySchema<TData>(schemaName));
            query.AddAttribute(PreparedQuery<TData>.QueryAttribute.Schema, schemaName);

            return query;
        }
    }
}
