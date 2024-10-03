namespace NTDLS.Katzebase.Engine.Functions.Procedures
{
    /// <summary>
    /// A parsed procedure parameter prototype
    /// </summary>
    internal class ProcedureParameterPrototype
    {
        public KbProcedureParameterType Type { get; private set; }
        public string Name { get; private set; }
        public string? DefaultValue { get; private set; }
        public bool HasDefault { get; private set; }

        public ProcedureParameterPrototype(KbProcedureParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;
        }

        public ProcedureParameterPrototype(KbProcedureParameterType type, string name, string? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
