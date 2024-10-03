using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a string constant.
    /// </summary>
    public class QueryFieldConstantString<TData> : IQueryField<TData> where TData : IStringable
    {
        public TData Value { get; set; }

        /// <summary>
        /// Not applicable to QueryFieldConstantString
        /// </summary>
        public string SchemaAlias { get; private set; } = string.Empty;

        public QueryFieldConstantString(TData value)
        {
            Value = value;
        }

        public IQueryField<TData> Clone()
        {
            var clone = new QueryFieldConstantString<TData>(Value)
            {
                SchemaAlias = SchemaAlias,
            };

            return clone;
        }
    }
}
