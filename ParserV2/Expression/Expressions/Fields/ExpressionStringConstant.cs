namespace ParserV2.Expression.Expressions.Fields
{
    /// <summary>
    /// Contains a string constant.
    /// </summary>
    internal class ExpressionStringConstant : IExpression
    {
        public string Value { get; set; }

        public ExpressionStringConstant(string value)
        {
            Value = value;
        }
    }
}
