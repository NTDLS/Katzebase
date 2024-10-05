namespace NTDLS.Katzebase.Parsers.Functions.Scalar
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    public class ScalarFunctionParameterPrototype
    {
        public KbScalarFunctionParameterType Type { get; private set; }
        public string Name { get; private set; }
        public string? DefaultValue { get; private set; }
        public bool HasDefault { get; private set; }

        public ScalarFunctionParameterPrototype(KbScalarFunctionParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;
        }

        public ScalarFunctionParameterPrototype(KbScalarFunctionParameterType type, string name, string? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
