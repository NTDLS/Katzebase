using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.Fields.Expressions
{
    /// <summary>
    /// Contains a string evaluation expression. This could be as simple as ["This" + "That"] or could contain function calls which child nodes.
    /// </summary>
    public class QueryFieldExpressionString<TData> : IQueryFieldExpression<TData> where TData : IStringable
    {
        public TData Value { get; set; } = default(TData);

        /// <summary>
        /// Not applicable to IQueryFieldExpression
        /// </summary>
        public string SchemaAlias { get; private set; } = string.Empty;

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        public List<IQueryFieldExpressionFunction> FunctionDependencies { get; set; } = new();

        public QueryFieldExpressionString()
        {
        }

        public IQueryField<TData> Clone()
        {
            var clone = new QueryFieldExpressionString<TData>()
            {
                Value = Value,
                SchemaAlias = SchemaAlias,
            };

            foreach (var functionDependency in FunctionDependencies)
            {
                clone.FunctionDependencies.Add(functionDependency.Clone());
            }

            return clone;
        }
    }
}
