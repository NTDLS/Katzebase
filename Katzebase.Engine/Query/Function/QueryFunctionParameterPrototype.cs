namespace Katzebase.Engine.Query.Function
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    internal class QueryFunctionParameterPrototype
    {
        public KbParameterType Type { get; set; }
        public string Name { get; set; }
        public string? DefaultValue { get; set; }

        public QueryFunctionParameterPrototype(KbParameterType type, string name)
        {
            Type = type;
            Name = name;
        }

        public QueryFunctionParameterPrototype(KbParameterType type, string name, string defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}
