﻿namespace NTDLS.Katzebase.Parsers.Functions
{
    /// <summary>
    /// This is a function parameter that contains the key of the other parameter which is the function that is required to be executed to satisfy this parameter.
    /// </summary>
    public class ExpressionFunctionParameterFunction : IExpressionFunctionParameter
    {
        public string Expression { get; set; }

        public ExpressionFunctionParameterFunction(string value)
        {
            Expression = value;
        }

        public IExpressionFunctionParameter Clone()
        {
            return new ExpressionFunctionParameterFunction(Expression);
        }
    }
}
