namespace NTDLS.Katzebase.PersistentTypes.Procedure
{
    public class PhysicalProcedureParameter
    {
        public KbProcedureParameterType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DefaultValue { get; set; }
        public bool HasDefault { get; set; } = false;
    }
}
