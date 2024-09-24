using fs;
namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    public class AggregateFunctionParameterValue
    {
        public AggregateFunctionParameterPrototype Parameter { get; private set; }
        public fstring? Value { get; private set; } = null;

        public AggregateFunctionParameterValue(AggregateFunctionParameterPrototype parameter, fstring? value)
        {
            Parameter = parameter;
            Value = value;
        }

    }
}
