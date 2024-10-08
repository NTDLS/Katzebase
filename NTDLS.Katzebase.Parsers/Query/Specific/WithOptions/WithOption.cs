namespace NTDLS.Katzebase.Parsers.Query.Specific.WithOptions
{
    public class WithOption(string name, object value, Type valueType)
    {
        public string Name { get; private set; } = name;
        public object Value { get; private set; } = value;
        public Type ValueType { get; private set; } = valueType;
    }
}
