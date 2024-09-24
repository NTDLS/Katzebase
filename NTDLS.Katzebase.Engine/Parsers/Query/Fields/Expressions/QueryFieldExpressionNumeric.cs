using fs;
namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions
{
    /// <summary>
    /// Contains a numeric evaluation expression. This could be as simple as [10 + 10] or could contain function calls which child nodes.
    /// </summary>
    internal class QueryFieldExpressionNumeric : IQueryFieldExpression
    {
        public fstring Value { get; set; }

        /// <summary>
        /// Not applicable to IQueryFieldExpression
        /// </summary>
        public string SchemaAlias { get; private set; } = string.Empty;

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        public List<IQueryFieldExpressionFunction> FunctionDependencies { get; private set; } = new();

        public QueryFieldExpressionNumeric(fstring value)
        {
            Value = value;
        }

        public QueryFieldExpressionNumeric(string value)
        {
            Value = fstring.NewS(value);
        }

        public IQueryField Clone()
        {
            var clone = new QueryFieldExpressionNumeric(Value)
            {
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
