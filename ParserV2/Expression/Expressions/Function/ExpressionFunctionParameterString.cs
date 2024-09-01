namespace ParserV2.Expression.Expressions.Function
{
    /// <summary>
    /// This is function call parameter that contains either a single string or a string expression (such as 'this' + 'that').
    /// </summary>
    internal class ExpressionFunctionParameterString : IExpressionFunctionParameter
    {
        public string Expression { get; set; }
        public List<ReferencedFunction> ReferencedFunctions { get; private set; } = new();

        public ExpressionFunctionParameterString(string value)
        {
            Expression = value;
        }
    }
}
