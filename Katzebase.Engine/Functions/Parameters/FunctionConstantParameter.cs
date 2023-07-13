namespace Katzebase.Engine.Functions.Parameters
{
    internal class FunctionConstantParameter : FunctionParameterBase
    {
        public string Value { get; set; } = string.Empty;

        public FunctionConstantParameter(string value)
        {
            Value = value;
        }
    }
}
