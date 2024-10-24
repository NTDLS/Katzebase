﻿using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Parsing.Root
{
    public static class StaticParserSample
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Sample, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Documents
            };

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{tokenizer.Variables.Resolve(schemaName)}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));

            if (tokenizer.TryEatIfNext("size"))
            {
                query.RowLimit = tokenizer.EatGetNextResolved<int>();
            }
            else
            {
                query.RowLimit = 100;
            }

            return query;
        }
    }
}
