namespace NTDLS.Katzebase.Parsers.Functions.System
{
    public class SystemFunctionParameterValue
    {
        public SystemFunctionParameterPrototype Parameter { get; private set; }
        public string? Value { get; private set; } = null;

        public SystemFunctionParameterValue(SystemFunctionParameterPrototype parameter, string? value)
        {
            Parameter = parameter;
            Value = value;
        }

        public SystemFunctionParameterValue(SystemFunctionParameterPrototype parameter)
        {
            Parameter = parameter;
        }
    }
}
