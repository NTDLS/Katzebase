namespace NTDLS.Katzebase.Engine.Parsers.Query.Class.WithOptions
{
    internal class WithOption
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
