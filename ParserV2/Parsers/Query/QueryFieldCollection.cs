using ParserV2.Parsers.Query.Fields;

namespace ParserV2.Parsers.Query
{
    /// <summary>
    /// Collection of query fields, which contains their names and values.
    /// </summary>
    internal class QueryFieldCollection : List<QueryField>
    {
        /// <summary>
        /// A list of all distinct document identifiers from all fields.
        /// We go out of our way to create this list because it helps optimize the query execution.
        /// </summary>
        public HashSet<QueryFieldDocumentIdentifier> DocumentIdentifiers { get; set; } = new();
    }
}
