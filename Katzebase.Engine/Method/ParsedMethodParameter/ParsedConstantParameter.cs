namespace Katzebase.Engine.Method.ParsedMethodParameter
{
    internal class ParsedConstantParameter : GenericParsedMethodParameter
    {
        public string Value { get; set; } = string.Empty;

        public ParsedConstantParameter(string value)
        {
            Value = value;
        }
    }
}
