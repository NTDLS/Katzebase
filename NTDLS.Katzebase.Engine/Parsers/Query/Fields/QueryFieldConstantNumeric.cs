namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a numeric constant.
    /// </summary>
    internal class QueryFieldConstantNumeric : IQueryField
    {
        public string Value { get; set; }

        /// <summary>
        /// Not applicable to QueryFieldConstantString
        /// </summary>
        public string SchemaAlias { get; private set; } = string.Empty;

        public QueryFieldConstantNumeric(string value)
        {
            Value = value;
        }

        public IQueryField Clone()
        {
            var clone = new QueryFieldConstantNumeric(Value)
            {
                SchemaAlias = SchemaAlias,
            };

            return clone;
        }
    }
}
