namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    internal class ScalerFunctionParameterValue
    {
        public ScalerFunctionParameterPrototype Parameter { get; set; }
        public string? Value { get; set; } = null;

        public ScalerFunctionParameterValue(ScalerFunctionParameterPrototype parameter, string? value)
        {
            Parameter = parameter;
            Value = value;
        }

        public ScalerFunctionParameterValue(ScalerFunctionParameterPrototype parameter)
        {
            Parameter = parameter;
        }
    }
}
