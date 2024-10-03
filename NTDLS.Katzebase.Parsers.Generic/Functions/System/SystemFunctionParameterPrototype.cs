using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Functions.System
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    public class SystemFunctionParameterPrototype<TData> where TData : IStringable
    {
        public KbSystemFunctionParameterType Type { get; private set; }
        public string Name { get; private set; }
        public TData? DefaultValue { get; private set; }
        public bool HasDefault { get; private set; }

        public SystemFunctionParameterPrototype(KbSystemFunctionParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;
        }

        public SystemFunctionParameterPrototype(KbSystemFunctionParameterType type, string name, TData? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
