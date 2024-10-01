using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserDropUniqueKey
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Drop)
            {
                SubQueryType = SubQueryType.Index
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var indexName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected index name, found: [{indexName}].");
            }

            query.AddAttribute(PreparedQuery.QueryAttribute.IndexName, indexName);
            query.AddAttribute(PreparedQuery.QueryAttribute.IsUnique, true);

            tokenizer.IsNext('(');

            var indexFields = tokenizer.EatGetMatchingScope().Split(',').Select(o => o.Trim()).ToList();
            query.CreateIndexFields.AddRange(indexFields);

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName));
            query.AddAttribute(PreparedQuery.QueryAttribute.Schema, schemaName);

            return query;
        }
    }
}
