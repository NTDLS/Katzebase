namespace ParserV2.Parsers.Query.Expressions.Fields
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
