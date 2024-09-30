using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserSet
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Set)
            {
                //SubQueryType = SubQueryType.None
            };

            string variableName = tokenizer.EatGetNext();
            string variableValue = tokenizer.EatGetNextEvaluated() ?? string.Empty;

            query.VariableValues.Add(new(variableName, variableValue));

            return query;
        }
    }
}
