using NTDLS.Katzebase.Parsers.Fields.Expressions;

namespace NTDLS.Katzebase.Parsers.SupportingTypes
{
    public class ExposedAggregateFunction
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
