namespace NTDLS.Katzebase.Parsers.Fields
{
    /// <summary>
    /// Contains a string constant.
    /// </summary>
    public class QueryFieldConstantString : IQueryField
    {
        public string? Value { get; set; }

        /// <summary>
        /// Not applicable to QueryFieldConstantString
        /// </summary>
        public string SchemaAlias { get; private set; } = string.Empty;

        /// <summary>
        /// If applicable, this is the line from the script that this expression is derived from.
        /// </summary>
        public int? ScriptLine { get; set; }

        public QueryFieldConstantString(int? scriptLine, string? value)
        {
            ScriptLine = scriptLine;
            Value = value;
        }

        public IQueryField Clone()
        {
            var clone = new QueryFieldConstantString(ScriptLine, Value)
            {
                SchemaAlias = SchemaAlias,
            };

            return clone;
        }
    }
}
