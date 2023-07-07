namespace Katzebase.Engine.Query.Function.Aggregate
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    internal class QueryAggregateFunctionParameterPrototype
    {
        public KbQueryAggregateFunctionParameterType Type { get; set; }
        public string Name { get; set; }
        public string? DefaultValue { get; set; }
        public bool HasDefault { get; set; }

        public QueryAggregateFunctionParameterPrototype(KbQueryAggregateFunctionParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;

        }

        public QueryAggregateFunctionParameterPrototype(KbQueryAggregateFunctionParameterType type, string name, string? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
