namespace ParserV2.Expression
{
    /// <summary>
    /// This is a function parameter that contains the key of the other parameter which is the function that is required to be executed to satisfy this parameter.
    /// </summary>
    internal class ExpressionFunctionParameterFunctionResult : IExpressionFunctionParameter
    {
        public string Expression { get; set; }

        public ExpressionFunctionParameterFunctionResult(string value)
        {
            Expression = value;
        }
    }
}
