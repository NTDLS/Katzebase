using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Parsers.Functions.Scaler
{
    public class ScalerFunctionParameterValue<TData> where TData : IStringable
    {
        public ScalerFunctionParameterPrototype<TData> Parameter { get; private set; }
        public TData? Value { get; private set; } = default(TData);

        public ScalerFunctionParameterValue(ScalerFunctionParameterPrototype<TData> parameter, TData? value)
        {
            Parameter = parameter;
            Value = value;
        }

        public ScalerFunctionParameterValue(ScalerFunctionParameterPrototype<TData> parameter)
        {
            Parameter = parameter;
        }
    }
}
