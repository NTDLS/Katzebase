﻿using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserGroupBy
    {
        public static QueryFieldCollection Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            return StaticParserFieldList.Parse(queryBatch, tokenizer, [" order ", " offset "], true);
        }
    }
}