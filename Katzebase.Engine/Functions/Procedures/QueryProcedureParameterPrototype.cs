namespace Katzebase.Engine.Functions.Procedures
{
    /// <summary>
    /// A parsed procedure parameter prototype
    /// </summary>
    internal class QueryProcedureParameterPrototype
    {
        public KbQueryProcedureParameterType Type { get; set; }
        public string Name { get; set; }
        public string? DefaultValue { get; set; }
        public bool HasDefault { get; set; }

        public QueryProcedureParameterPrototype(KbQueryProcedureParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;
        }

        public QueryProcedureParameterPrototype(KbQueryProcedureParameterType type, string name, string? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
