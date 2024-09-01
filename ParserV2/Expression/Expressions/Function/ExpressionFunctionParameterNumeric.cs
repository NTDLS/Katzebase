namespace ParserV2.Expression.Expressions.Function
{
    /// <summary>
    /// This is function call parameter that contains either a single numeric value or an expression consisting solely of numeric operations.
    /// </summary>
    internal class ExpressionFunctionParameterNumeric : IExpressionFunctionParameter
    {
        public string Expression { get; set; }
        public List<ReferencedFunction> ReferencedFunctions { get; private set; } = new();

        public ExpressionFunctionParameterNumeric(string value)
        {
            Expression = value;
        }
    }
}
