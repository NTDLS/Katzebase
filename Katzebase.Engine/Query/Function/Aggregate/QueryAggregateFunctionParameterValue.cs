namespace Katzebase.Engine.Query.Function.Aggregate
{
    internal class QueryAggregateFunctionParameterValue
    {
        public QueryAggregateFunctionParameterPrototype Parameter { get; set; }
        public AggregateGenericParameter? Value { get; set; } = null;

        public QueryAggregateFunctionParameterValue(QueryAggregateFunctionParameterPrototype parameter, AggregateGenericParameter? value)
        {
            Parameter = parameter;
            Value = value;
        }

    }
}
