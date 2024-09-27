namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    public class AggregateFunctionParameterValue<TData> where TData : IStringable
    {
        public AggregateFunctionParameterPrototype Parameter { get; private set; }
        public TData? Value { get; private set; } = default;

        public AggregateFunctionParameterValue(AggregateFunctionParameterPrototype parameter, TData? value)
        {
            Parameter = parameter;
            Value = value;
        }

    }
}
