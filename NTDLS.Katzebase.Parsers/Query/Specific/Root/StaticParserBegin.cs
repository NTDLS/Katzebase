﻿using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific.Root
{
    public static class StaticParserBegin
    {
        internal static SupportingTypes.Query Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new SupportingTypes.Query(queryBatch, QueryType.Begin, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = tokenizer.EatIfNextEnum([SubQueryType.Transaction])
            };

            return query;
        }
    }
}
