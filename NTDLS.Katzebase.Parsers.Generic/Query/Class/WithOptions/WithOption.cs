namespace NTDLS.Katzebase.Parsers.Query.Class.WithOptions
{
    public class WithOption
    {
        public string Name { get; private set; }
        public object Value { get; private set; }
        public Type ValueType { get; private set; }

        public WithOption(string name, object value, Type valueType)
        {
            Name = name;
            Value = value;
            ValueType = valueType;
        }
    }
}
