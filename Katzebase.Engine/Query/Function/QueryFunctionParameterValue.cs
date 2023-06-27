namespace Katzebase.Engine.Query.Function
{
    internal class QueryFunctionParameterValue
    {
        public QueryFunctionParameterPrototype Parameter { get; set; }
        public string? Value { get; set; } = null;

        public QueryFunctionParameterValue(QueryFunctionParameterPrototype parameter, string value)
        {
            Parameter = parameter;
            Value = value;
        }

        public QueryFunctionParameterValue(QueryFunctionParameterPrototype parameter)
        {
            Parameter = parameter;
        }
    }
}
