namespace NTDLS.Katzebase.Parsers.Query.Fields.Expressions
{
    /// <summary>
    /// Contains a string evaluation expression. This could be as simple as ["This" + "That"] or could contain function calls which child nodes.
    /// </summary>
    public class QueryFieldExpressionString : IQueryFieldExpression
    {
        public string? Value { get; set; } = string.Empty;

        /// <summary>
        /// Not applicable to IQueryFieldExpression
        /// </summary>
        public string SchemaAlias { get; private set; } = string.Empty;

        /// <summary>
        /// If applicable, this is the line from the script that this expression is derived from.
        /// </summary>
        public int? ScriptLine { get; set; }

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        public List<IQueryFieldExpressionFunction> FunctionDependencies { get; set; } = new();

        public QueryFieldExpressionString(int? scriptLine, string? value)
        {
            ScriptLine = scriptLine;
            Value = value;
        }

        public IQueryField Clone()
        {
            var clone = new QueryFieldExpressionString(ScriptLine, Value)
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
