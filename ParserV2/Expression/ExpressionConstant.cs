namespace ParserV2.Expression
{
    internal class ExpressionConstant : IExpression
    {
        public enum ExpressionConstantType
        {
            String,
            Numeric
        }

        public ExpressionConstantType ConstantType { get; set; }

        public string Value { get; set; }

        public ExpressionConstant(string value, ExpressionConstantType constantType)
        {
            Value = value;
            ConstantType = constantType;

        }
    }
}
