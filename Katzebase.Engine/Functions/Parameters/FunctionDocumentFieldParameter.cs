using Katzebase.Engine.Query;

namespace Katzebase.Engine.Functions.Parameters
{
    internal class FunctionDocumentFieldParameter : FunctionParameterBase
    {
        public PrefixedField Value { get; set; }
        public string ExpressionKey { get; set; } = string.Empty;

        public FunctionDocumentFieldParameter(string value)
        {
            Value = PrefixedField.Parse(value);
        }
    }
}
