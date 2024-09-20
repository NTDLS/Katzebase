using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Exposed
{

    internal class ExposedAggregateFunction
    {
        public QueryFieldExpressionFunctionAggregate Function { get; set; }

        public List<IQueryFieldExpressionFunction> FunctionDependencies { get; set; }


        public ExposedAggregateFunction(QueryFieldExpressionFunctionAggregate function, List<IQueryFieldExpressionFunction> functionDependencies)
        {
            Function = function;
            FunctionDependencies = functionDependencies;
        }
    }
}
