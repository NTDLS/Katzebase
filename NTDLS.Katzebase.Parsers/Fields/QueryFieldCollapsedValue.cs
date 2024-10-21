namespace NTDLS.Katzebase.Parsers.Fields
{
    /// <summary>
    /// Contains a pre-collapsed value.
    /// </summary>
    public class QueryFieldCollapsedValue : IQueryField
    {
        public string? Value { get; set; }
        public string SchemaAlias { get; private set; } = string.Empty;

        /// <summary>
        /// If applicable, this is the line from the script that this expression is derived from.
        /// </summary>
        public int? ScriptLine { get; set; }

        public QueryFieldCollapsedValue(int? scriptLine, string? value)
        {
            ScriptLine = scriptLine;
            Value = value;
        }

        public IQueryField Clone()
        {
            var clone = new QueryFieldCollapsedValue(ScriptLine, Value)
            {
                SchemaAlias = SchemaAlias,
            };

            return clone;
        }
    }
}
