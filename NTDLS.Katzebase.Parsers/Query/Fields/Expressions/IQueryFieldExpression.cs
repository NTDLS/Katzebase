namespace NTDLS.Katzebase.Parsers.Query.Fields.Expressions
{
    public interface IQueryFieldExpression : IQueryField
    {
        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        List<IQueryFieldExpressionFunction> FunctionDependencies { get; }
    }
}
