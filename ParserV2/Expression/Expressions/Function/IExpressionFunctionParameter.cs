namespace ParserV2.Expression.Expressions.Function
{
    internal interface IExpressionFunctionParameter
    {
        string Expression { get; set; }
        List<ReferencedFunction> ReferencedFunctions { get; }
    }
}
