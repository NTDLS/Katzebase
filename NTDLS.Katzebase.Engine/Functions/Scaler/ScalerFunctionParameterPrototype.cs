using fs;

namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    public class ScalerFunctionParameterPrototype
    {
        public KbScalerFunctionParameterType Type { get; private set; }
        public string Name { get; private set; }
        public fstring? DefaultValue { get; private set; }
        public bool HasDefault { get; private set; }

        public ScalerFunctionParameterPrototype(KbScalerFunctionParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;
        }

        public ScalerFunctionParameterPrototype(KbScalerFunctionParameterType type, string name, fstring? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
