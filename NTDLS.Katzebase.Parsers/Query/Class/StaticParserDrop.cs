﻿using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserDrop
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Schema, SubQueryType.Index, SubQueryType.UniqueKey, SubQueryType.Procedure]);

            return querySubType switch
            {
                SubQueryType.Schema => StaticParserDropSchema.Parse(queryBatch, tokenizer),
                SubQueryType.Index => StaticParserDropIndex.Parse(queryBatch, tokenizer),
                SubQueryType.UniqueKey => StaticParserDropUniqueKey.Parse(queryBatch, tokenizer),
                SubQueryType.Procedure => StaticParserDropProcedure.Parse(queryBatch, tokenizer),

                _ => throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Query type is not implemented: [{querySubType}].")
            };
        }
    }
}
