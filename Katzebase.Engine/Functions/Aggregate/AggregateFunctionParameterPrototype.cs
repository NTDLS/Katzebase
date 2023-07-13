namespace Katzebase.Engine.Functions.Aggregate
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    internal class AggregateFunctionParameterPrototype
    {
        public KbQueryAggregateFunctionParameterType Type { get; set; }
        public string Name { get; set; }
        public string? DefaultValue { get; set; }
        public bool HasDefault { get; set; }

        public AggregateFunctionParameterPrototype(KbQueryAggregateFunctionParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;

        }

        public AggregateFunctionParameterPrototype(KbQueryAggregateFunctionParameterType type, string name, string? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
