using fs;

namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    public class ScalerFunctionParameterValue
    {
        public ScalerFunctionParameterPrototype Parameter { get; private set; }
        public fstring? Value { get; private set; } = null;

        public ScalerFunctionParameterValue(ScalerFunctionParameterPrototype parameter, fstring? value)
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
