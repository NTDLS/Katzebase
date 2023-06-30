namespace Katzebase.Engine.Query.FunctionParameter
{
    internal class FunctionMethodAndParams : FunctionParameterBase
    {
        public string Method { get; set; } = string.Empty;
        public List<FunctionParameterBase> Parameters { get; set; } = new();
    }
}
