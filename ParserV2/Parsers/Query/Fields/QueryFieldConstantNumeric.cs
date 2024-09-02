namespace ParserV2.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a numeric constant.
    /// </summary>
    internal class QueryFieldConstantNumeric : IQueryField
    {
        public string Value { get; set; }

        public QueryFieldConstantNumeric(string value)
        {
            Value = value;
        }
    }
}
