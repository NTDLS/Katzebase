namespace ParserV2.Parsers.Query.Fields.Expressions
{
    internal interface IQueryFieldExpression : IQueryField
    {
        string Expression { get; set; }
        string GetKeyExpressionKey();

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        List<QueryFieldExpressionFunction> FunctionDependencies { get; }
    }
}
