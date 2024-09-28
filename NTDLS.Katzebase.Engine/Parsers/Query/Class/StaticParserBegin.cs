using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserBegin<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            var query = new PreparedQuery<TData>(queryBatch, QueryType.Begin)
            {
                SubQueryType = tokenizer.EatIfNextEnum([SubQueryType.Transaction])
            };

            return query;
        }
    }
}
