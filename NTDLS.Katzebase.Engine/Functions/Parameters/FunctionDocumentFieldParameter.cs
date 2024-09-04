using NTDLS.Katzebase.Engine.Query.SupportingTypes;

namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    internal class FunctionDocumentFieldParameter : FunctionParameterBase
    {
        public PrefixedField Value { get; private set; }
        public string ExpressionKey { get; private set; } = string.Empty;

        public FunctionDocumentFieldParameter(string value)
        {
            Value = PrefixedField.Parse(value);
        }

        public FunctionDocumentFieldParameter(string value, string expressionKey)
        {
            Value = PrefixedField.Parse(value);
            ExpressionKey = expressionKey;
        }

    }
}
