namespace ParserV2.Parsers.Query.Expressions.Fields
{
    /// <summary>
    /// Contains a numeric constant.
    /// </summary>
    internal class ExpressionNumericConstant : IExpression
    {
        public string Value { get; set; }

        public ExpressionNumericConstant(string value)
        {
            Value = value;
        }
    }
}
