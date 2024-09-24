using fs;
namespace NTDLS.Katzebase.Engine.Functions.System
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    public class SystemFunctionParameterPrototype
    {
        public KbSystemFunctionParameterType Type { get; private set; }
        public string Name { get; private set; }
        public fstring? DefaultValue { get; private set; }
        public bool HasDefault { get; private set; }

        public SystemFunctionParameterPrototype(KbSystemFunctionParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;
        }

        public SystemFunctionParameterPrototype(KbSystemFunctionParameterType type, string name, fstring? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
