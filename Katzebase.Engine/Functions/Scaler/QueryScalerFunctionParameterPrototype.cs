namespace Katzebase.Engine.Functions.Scaler
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    internal class QueryScalerFunctionParameterPrototype
    {
        public KbQueryScalerFunctionParameterType Type { get; set; }
        public string Name { get; set; }
        public string? DefaultValue { get; set; }
        public bool HasDefault { get; set; }

        public QueryScalerFunctionParameterPrototype(KbQueryScalerFunctionParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;

        }

        public QueryScalerFunctionParameterPrototype(KbQueryScalerFunctionParameterType type, string name, string? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
