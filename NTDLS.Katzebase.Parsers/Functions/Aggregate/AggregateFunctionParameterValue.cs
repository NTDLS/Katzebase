namespace NTDLS.Katzebase.Parsers.Functions.Aggregate
{
    public class AggregateFunctionParameterValue(AggregateFunctionParameterPrototype parameter, string? value)
    {
        public AggregateFunctionParameterPrototype Parameter { get; private set; } = parameter;
        public string? Value { get; private set; } = value;
    }
}
