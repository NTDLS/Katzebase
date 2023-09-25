namespace NTDLS.Katzebase.Engine.Functions.Procedures.Persistent
{
    public class PhysicalProcedureParameter
    {
        public KbProcedureParameterType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DefaultValue { get; set; }
        public bool HasDefault { get; set; } = false;


        public PhysicalProcedureParameter()
        {
        }

        internal ProcedureParameterPrototype ToProcedureParameterPrototype()
        {
            return new ProcedureParameterPrototype(Type, Name, DefaultValue);
        }

        public PhysicalProcedureParameter(string name, KbProcedureParameterType type)
        {
            Name = name;
            Type = type;
        }
    }
}
