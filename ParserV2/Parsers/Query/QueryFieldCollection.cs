namespace ParserV2.Parsers.Query
{
    /// <summary>
    /// Collection of query fields, which contains their names and values.
    /// </summary>
    internal class QueryFieldCollection
    {
        /// <summary>
        /// The list of query fields, which contains their names and values.
        /// </summary>
        public List<QueryField> Collection { get; set; } = new();
    }
}
