namespace NTDLS.Katzebase.Engine.Parsers.Query.Functions
{
    /// <summary>
    /// This is function call parameter that contains either a single numeric value or an expression consisting solely of numeric operations.
    /// </summary>
    internal class ExpressionFunctionParameterNumeric : IExpressionFunctionParameter
    {
        public string Expression { get; set; }

        public ExpressionFunctionParameterNumeric(string value)
        {
            Expression = value;
        }

        public IExpressionFunctionParameter Clone()
        {
            return new ExpressionFunctionParameterNumeric(Expression);
        }
    }
}
