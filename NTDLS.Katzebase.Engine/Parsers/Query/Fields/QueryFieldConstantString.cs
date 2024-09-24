using fs;
namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a string constant.
    /// </summary>
    internal class QueryFieldConstantString : IQueryField
    {
        public fstring Value { get; set; }

        /// <summary>
        /// Not applicable to QueryFieldConstantString
        /// </summary>
        public string SchemaAlias { get; private set; } = string.Empty;

        public QueryFieldConstantString(fstring value)
        {
            Value = value;
        }

        public QueryFieldConstantString(string value)
        {
            Value = fstring.NewS(value);
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
