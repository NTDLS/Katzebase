﻿using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Parsing
{
    public static class StaticParserAnalyzeIndex
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Analyze, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Index
            };

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var indexName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected index name, found: [{indexName}].");
            }
            query.AddAttribute(PreparedQuery.Attribute.IndexName, indexName);

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));
            query.AddAttribute(PreparedQuery.Attribute.Schema, schemaName);
            return query;
        }
    }
}
