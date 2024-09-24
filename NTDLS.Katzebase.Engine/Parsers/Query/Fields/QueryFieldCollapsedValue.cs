using fs;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a pre-collapsed value.
    /// </summary>
    internal class QueryFieldCollapsedValue : IQueryField
    {
        public fstring Value { get; set; }
        public string SchemaAlias { get; private set; } = string.Empty;

        public QueryFieldCollapsedValue(fstring value)
        {
            Value = value;
        }

        public IQueryField Clone()
        {
            var clone = new QueryFieldCollapsedValue(Value)
            {
                SchemaAlias = SchemaAlias,
            };

            return clone;
        }
    }
}
