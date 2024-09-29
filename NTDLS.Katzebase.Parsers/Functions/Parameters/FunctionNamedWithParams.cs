namespace NTDLS.Katzebase.Parsers.Functions.Parameters
{
    public class FunctionNamedWithParams : FunctionWithParams
    {
        public string ExpressionKey { get; set; } = string.Empty;

        public FunctionNamedWithParams(string functionName) : base(functionName)
        {
        }
    }
}
