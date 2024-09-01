namespace ParserV2.Expression
{
    internal interface IExpressionEvaluation : IExpression
    {
        string Expression { get; set; }
        string GetKeyExpressionKey();

        List<FunctionCallEvaluation> FunctionCalls { get; }
        List<ReferencedFunction> ReferencedFunctions { get; }
    }
}
