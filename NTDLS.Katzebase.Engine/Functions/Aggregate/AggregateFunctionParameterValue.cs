namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    public class AggregateFunctionParameterValue
    {
        public AggregateFunctionParameterPrototype Parameter { get; private set; }
        public string? Value { get; private set; } = null;

        public AggregateFunctionParameterValue(AggregateFunctionParameterPrototype parameter, string? value)
        {
            Parameter = parameter;
            Value = value;
        }

    }
}
