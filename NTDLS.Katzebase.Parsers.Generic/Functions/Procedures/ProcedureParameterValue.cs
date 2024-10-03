namespace NTDLS.Katzebase.Engine.Functions.Procedures
{
    internal class ProcedureParameterValue
    {
        public ProcedureParameterPrototype Parameter { get; private set; }
        public string? Value { get; private set; } = null;

        public ProcedureParameterValue(ProcedureParameterPrototype parameter, string? value)
        {
            Parameter = parameter;
            Value = value;
        }

        public ProcedureParameterValue(ProcedureParameterPrototype parameter)
        {
            Parameter = parameter;
        }
    }
}
