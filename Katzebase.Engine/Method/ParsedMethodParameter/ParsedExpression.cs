namespace Katzebase.Engine.Method.ParsedMethodParameter
{
    internal class ParsedExpression : GenericParsedMethodParameter
    {
        public string Value { get; set; } = string.Empty;
        public List<GenericParsedMethodParameter> Parameters { get; set; } = new();
    }
}
