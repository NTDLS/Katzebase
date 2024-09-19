using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserBegin
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Begin)
            {
                SubQueryType = tokenizer.EatIfNextEnum([SubQueryType.Transaction])
            };

            return query;
        }
    }
}
