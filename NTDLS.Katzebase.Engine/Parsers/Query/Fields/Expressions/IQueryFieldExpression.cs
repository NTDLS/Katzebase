namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions
{
    public interface IQueryFieldExpression : IQueryField
    {
        string Expression { get; set; }
        string GetKeyExpressionKey();

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        List<IQueryFieldExpressionFunction> FunctionDependencies { get; }
    }
}
