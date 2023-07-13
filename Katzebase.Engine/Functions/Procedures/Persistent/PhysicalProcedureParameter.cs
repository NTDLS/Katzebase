namespace Katzebase.Engine.Functions.Procedures.Persistent
{
    public class PhysicalProcedureParameter
    {
        public string Name { get; set; } = string.Empty;
        public KbProcedureParameterType Type { get; set; }

        public PhysicalProcedureParameter()
        {
        }
        public PhysicalProcedureParameter(string name, KbProcedureParameterType type)
        {
            Name = name;
            Type = type;
        }
    }
}
