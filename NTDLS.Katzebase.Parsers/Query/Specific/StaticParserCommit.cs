using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserCommit
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Commit, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = tokenizer.EatIfNextEnum([SubQueryType.Transaction])
            };

            return query;
        }
    }
}
