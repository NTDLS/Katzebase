using fs;
namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    public class AggregateFunctionParameterPrototype
    {
        public KbAggregateFunctionParameterType Type { get; private set; }
        public string Name { get; private set; }
        public fstring? DefaultValue { get; private set; }
        public bool HasDefault { get; private set; }

        public AggregateFunctionParameterPrototype(KbAggregateFunctionParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;
        }

        public AggregateFunctionParameterPrototype(KbAggregateFunctionParameterType type, string name, fstring? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
