namespace ParserV2.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a string constant.
    /// </summary>
    internal class QueryFieldConstantString : IQueryField
    {
        public string Value { get; set; }

        public QueryFieldConstantString(string value)
        {
            Value = value;
        }
    }
}
