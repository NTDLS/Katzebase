using Katzebase.Engine.Query;

namespace Katzebase.Engine.Method.ParsedMethodParameter
{
    internal class ParsedFieldParameter : GenericParsedMethodParameter
    {
        public PrefixedField Value { get; set; }
        public string ExpressionKey { get; set; } = string.Empty;

        public ParsedFieldParameter(string value)
        {
            Value = PrefixedField.Parse(value);
        }
    }
}
