﻿using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserExec
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Exec)
            {
                //SubQueryType = SubQueryType.None
            };

            throw new NotImplementedException("reimplement");

            /*
                result.ProcedureCall = StaticFunctionParsers.ParseProcedureParameters(tokenizer);
            */

            return query;
        }
    }
}
