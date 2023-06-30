namespace Katzebase.Engine.Query.Function.Scaler
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    internal class QueryScalerFunctionParameterPrototype
    {
        public KbParameterType Type { get; set; }
        public string Name { get; set; }
        public string? DefaultValue { get; set; }

        public QueryScalerFunctionParameterPrototype(KbParameterType type, string name)
        {
            Type = type;
            Name = name;
        }

        public QueryScalerFunctionParameterPrototype(KbParameterType type, string name, string defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}
