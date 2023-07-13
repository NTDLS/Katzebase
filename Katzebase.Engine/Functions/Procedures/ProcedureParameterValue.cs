namespace Katzebase.Engine.Functions.Procedures
{
    internal class ProcedureParameterValue
    {
        public ProcedureParameterPrototype Parameter { get; set; }
        public string? Value { get; set; } = null;

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
