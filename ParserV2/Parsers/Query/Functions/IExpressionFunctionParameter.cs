namespace ParserV2.Parsers.Query.Functions
{
    internal interface IExpressionFunctionParameter
    {
        string Expression { get; set; }
        List<FunctionReference> ReferencedFunctions { get; }
    }
}
