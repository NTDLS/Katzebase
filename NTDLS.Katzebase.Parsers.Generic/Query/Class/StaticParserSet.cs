using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using NTDLS.Katzebase.Parsers;

namespace NTDLS.Katzebase.Parsers.Query.Class
    
{
    public class StaticParserSet 
    {
        internal static PreparedQuery<TData> Parse<TData>(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer) where TData : IStringable
        {
            var query = new PreparedQuery<TData>(queryBatch, Constants.QueryType.Set)
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
