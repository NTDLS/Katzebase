using NTDLS.Katzebase.Engine.Parsers.Query.Fields;

namespace NTDLS.Katzebase.Engine.Parsers.Query
{
    /// <summary>
    /// Collection of query fields, which contains their names and values.
    /// </summary>
    public class QueryFieldCollection : List<QueryField>
    {
        private int nextFieldAlias = 0;

        /// <summary>
        /// A list of all distinct document identifiers from all fields.
        /// We go out of our way to create this list because it helps optimize the query execution.
        /// </summary>
        public HashSet<QueryFieldDocumentIdentifier> DocumentIdentifiers { get; set; } = new();

        /// <summary>
        /// Gets a field alias for a field for which the query did not supply an alias.
        /// </summary>
        /// <returns></returns>
        public string GetNextFieldAlias()
            => $"Expression{nextFieldAlias++}";
    }
}
