namespace Katzebase.Engine.Method.ParsedMethodParameter
{
    internal class ParsedMethodAndParams : GenericParsedMethodParameter
    {
        public string Method { get; set; } = string.Empty;
        public List<GenericParsedMethodParameter> Parameters { get; set; } = new();
    }
}
