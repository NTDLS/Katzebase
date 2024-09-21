using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserSet
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Set)
            {
                //SubQueryType = SubQueryType.None
            };

            throw new NotImplementedException("reimplement");

            /*
                string variableName = tokenizer.GetNext();
                string variableValue = tokenizer.GetNext();
                result.VariableValues.Add(new(variableName, variableValue));
            */
        }
    }
}
