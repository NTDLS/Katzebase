namespace NTDLS.Katzebase.Parsers.Functions
{
    /// <summary>
    /// This is function call parameter that contains either a single numeric value or an expression consisting solely of numeric operations.
    /// </summary>
    public class ExpressionFunctionParameterNumeric : IExpressionFunctionParameter
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
