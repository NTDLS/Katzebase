namespace Katzebase.Engine.Query.Function.Scaler
{
    internal class QueryScalerFunctionParameterValue
    {
        public QueryScalerFunctionParameterPrototype Parameter { get; set; }
        public string? Value { get; set; } = null;

        public QueryScalerFunctionParameterValue(QueryScalerFunctionParameterPrototype parameter, string? value)
        {
            Parameter = parameter;
            Value = value;
        }

        public QueryScalerFunctionParameterValue(QueryScalerFunctionParameterPrototype parameter)
        {
            Parameter = parameter;
        }
    }
}
