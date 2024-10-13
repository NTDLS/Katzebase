using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific.Root
{
    public static class StaticParserSet
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Set, tokenizer.GetCurrentLineNumber())
            {
                //SubQueryType = SubQueryType.None
            };

            string variableName = tokenizer.EatGetNext();
            string variableValue = tokenizer.EatGetNextResolved() ?? string.Empty;

            query.VariableValues.Add(new(variableName, variableValue));

            return query;
        }
    }
}
