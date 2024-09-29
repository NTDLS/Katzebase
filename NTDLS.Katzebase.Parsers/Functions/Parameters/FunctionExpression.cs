namespace NTDLS.Katzebase.Parsers.Functions.Parameters
{
    public class FunctionExpression : FunctionParameterBase
    {
        public enum FunctionExpressionType
        {
            Text,
            Numeric
        }

        public FunctionExpressionType ExpressionType { get; set; }
        public string Value { get; set; } = string.Empty;
        public List<FunctionParameterBase> Parameters { get; private set; } = new();
    }
}
