namespace NTDLS.Katzebase.Engine.Parsers.Query.Functions
{
    /// <summary>
    /// This is function call parameter that contains either a single string or a string expression (such as 'this' + 'that').
    /// </summary>
    public class ExpressionFunctionParameterString : IExpressionFunctionParameter
    {
        public string Expression { get; set; }

        public ExpressionFunctionParameterString(string value)
        {
            Expression = value;
        }
    }
}
