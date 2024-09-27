namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a numeric constant.
    /// </summary>
    internal class QueryFieldConstantNumeric<TData> : IQueryField<TData> where TData : IStringable
    {
        public TData Value { get; set; }

        /// <summary>
        /// Not applicable to QueryFieldConstantString
        /// </summary>
        public string SchemaAlias { get; private set; } = string.Empty;

        public QueryFieldConstantNumeric(TData value)
        {
            Value = value;
        }

        public IQueryField<TData> Clone()
        {
            var clone = new QueryFieldConstantNumeric<TData>(Value)
            {
                SchemaAlias = SchemaAlias,
            };

            return clone;
        }
    }
}
