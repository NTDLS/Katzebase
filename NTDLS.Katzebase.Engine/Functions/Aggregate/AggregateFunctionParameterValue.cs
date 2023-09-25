using NTDLS.Katzebase.Engine.Functions.Aggregate.Parameters;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    internal class AggregateFunctionParameterValue
    {
        public AggregateFunctionParameterPrototype Parameter { get; set; }
        public AggregateGenericParameter? Value { get; set; } = null;

        public AggregateFunctionParameterValue(AggregateFunctionParameterPrototype parameter, AggregateGenericParameter? value)
        {
            Parameter = parameter;
            Value = value;
        }

    }
}
