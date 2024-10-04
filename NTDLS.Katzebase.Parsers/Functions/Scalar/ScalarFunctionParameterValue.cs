namespace NTDLS.Katzebase.Parsers.Functions.Scalar
{
    public class ScalarFunctionParameterValue
    {
        public ScalarFunctionParameterPrototype Parameter { get; private set; }
        public string? Value { get; private set; } = null;

        public ScalarFunctionParameterValue(ScalarFunctionParameterPrototype parameter, string? value)
        {
            Parameter = parameter;
            Value = value;
        }

        public ScalarFunctionParameterValue(ScalarFunctionParameterPrototype parameter)
        {
            Parameter = parameter;
        }
    }
}
