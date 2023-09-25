namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    /// <summary>
    /// A parsed function parameter prototype
    /// </summary>
    internal class ScalerFunctionParameterPrototype
    {
        public KbScalerFunctionParameterType Type { get; set; }
        public string Name { get; set; }
        public string? DefaultValue { get; set; }
        public bool HasDefault { get; set; }

        public ScalerFunctionParameterPrototype(KbScalerFunctionParameterType type, string name)
        {
            Type = type;
            Name = name;
            HasDefault = false;

        }

        public ScalerFunctionParameterPrototype(KbScalerFunctionParameterType type, string name, string? defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            HasDefault = true;
        }
    }
}
