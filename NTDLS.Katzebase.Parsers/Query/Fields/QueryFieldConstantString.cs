namespace NTDLS.Katzebase.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a string constant.
    /// </summary>
    public class QueryFieldConstantString : IQueryField
    {
        public string Value { get; set; }

        /// <summary>
        /// Not applicable to QueryFieldConstantString
        /// </summary>
        public string SchemaAlias { get; private set; } = string.Empty;

        public QueryFieldConstantString(string value)
        {
            Value = value;
        }

        public IQueryField Clone()
        {
            var clone = new QueryFieldConstantString(Value)
            {
                SchemaAlias = SchemaAlias,
            };

            return clone;
        }
    }
}
