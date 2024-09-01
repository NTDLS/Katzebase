namespace ParserV2.Parsers.Query.Expressions.Function
{
    internal interface IExpressionFunctionParameter
    {
        string Expression { get; set; }
        List<ReferencedFunction> ReferencedFunctions { get; }
    }
}
