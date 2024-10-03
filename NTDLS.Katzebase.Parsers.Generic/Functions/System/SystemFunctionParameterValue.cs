using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Functions.System
{
    public class SystemFunctionParameterValue<TData> where TData : IStringable
    {
        public SystemFunctionParameterPrototype<TData> Parameter { get; private set; }
        public TData? Value { get; private set; } = default;

        public SystemFunctionParameterValue(SystemFunctionParameterPrototype<TData> parameter, TData? value)
        {
            Parameter = parameter;
            Value = value;
        }

        public SystemFunctionParameterValue(SystemFunctionParameterPrototype<TData> parameter)
        {
            Parameter = parameter;
        }
    }
}
