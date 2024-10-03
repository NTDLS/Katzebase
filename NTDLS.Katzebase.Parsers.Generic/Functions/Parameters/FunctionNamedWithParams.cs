namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    internal class FunctionNamedWithParams : FunctionWithParams
    {
        public string ExpressionKey { get; set; } = string.Empty;

        public FunctionNamedWithParams(string functionName) : base(functionName)
        {
        }
    }
}
