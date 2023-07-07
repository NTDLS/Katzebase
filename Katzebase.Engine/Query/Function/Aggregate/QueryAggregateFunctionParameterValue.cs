namespace Katzebase.Engine.Query.Function.Aggregate
{
    internal class QueryAggregateFunctionParameterValue
    {
        public QueryAggregateFunctionParameterPrototype Parameter { get; set; }
        public string? Value { get; set; } = null;

        public QueryAggregateFunctionParameterValue(QueryAggregateFunctionParameterPrototype parameter, string? value)
        {
            Parameter = parameter;
            Value = value;
        }

        public QueryAggregateFunctionParameterValue(QueryAggregateFunctionParameterPrototype parameter)
        {
            Parameter = parameter;
        }
    }
}
