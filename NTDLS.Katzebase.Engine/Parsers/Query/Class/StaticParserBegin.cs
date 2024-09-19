using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserBegin
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token = tokenizer.EatGetNext();

            if (StaticParserUtility.IsStartOfQuery(token, out var queryType) == false)
            {
                string acceptableValues = string.Join("', '", Enum.GetValues<QueryType>().Where(o => o != QueryType.None));
                throw new KbParserException($"Invalid query. Found '{token}', expected: '{acceptableValues}'.");
            }

            var query = new PreparedQuery(queryBatch, queryType);

            tokenizer.EatIfNext("transaction");

            query.SubQueryType = SubQueryType.Transaction;

            return query;
        }
    }
}
