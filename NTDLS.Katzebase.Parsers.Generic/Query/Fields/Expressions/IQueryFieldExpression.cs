using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.Fields.Expressions
{
    public interface IQueryFieldExpression<TData> : IQueryField<TData> where TData : IStringable
    {
        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        List<IQueryFieldExpressionFunction> FunctionDependencies { get; }
    }
}
