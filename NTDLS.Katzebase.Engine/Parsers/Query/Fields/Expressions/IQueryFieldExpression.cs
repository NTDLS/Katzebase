namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions
{
    internal interface IQueryFieldExpression<TData> : IQueryField<TData> where TData : IStringable
    {
        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        List<IQueryFieldExpressionFunction> FunctionDependencies { get; }
    }
}
