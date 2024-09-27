namespace NTDLS.Katzebase.Engine.Functions.System
{
    public class SystemFunctionParameterValue<TData> where TData : IStringable
    {
        public SystemFunctionParameterPrototype Parameter { get; private set; }
        public TData? Value { get; private set; } = default;

        public SystemFunctionParameterValue(SystemFunctionParameterPrototype parameter, TData? value)
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
