using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Root
{
    public static class StaticParserCommit
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Commit, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = tokenizer.EatIfNextEnum([SubQueryType.Transaction])
            };

            return query;
        }
    }
}
