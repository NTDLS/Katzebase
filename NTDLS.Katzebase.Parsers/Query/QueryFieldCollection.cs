using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;

namespace NTDLS.Katzebase.Parsers.Query
{
    /// <summary>
    /// Collection of query fields, which contains their names and values.
    /// </summary>
    public class QueryFieldCollection : List<QueryField>
    {
        public QueryBatch QueryBatch { get; private set; }

        /// <summary>
        /// A list of all distinct document identifiers from all fields, even nested expressions.
        /// We go out of our way to create this list because it helps optimize the query execution.
        /// </summary>
        public KbInsensitiveDictionary<QueryFieldDocumentIdentifier> DocumentIdentifiers { get; set; } = new();

        /// <summary>
        /// Gets a field alias for a field for which the query did not supply an alias.
        /// </summary>
        /// <returns></returns>
        public string GetNextFieldAlias()
            => $"Expression{nextFieldAlias++}";
        private int nextFieldAlias = 0;

        public string GetNextExpressionKey()
            => $"$x_{_nextExpressionKey++}$";
        private int _nextExpressionKey = 0;

        /// <summary>
        /// Get a document field placeholder.
        /// </summary>
        /// <returns></returns>
        public string GetNextDocumentFieldKey()
            => $"$f_{_nextDocumentFieldKey++}$";
        private int _nextDocumentFieldKey = 0;

        public QueryFieldCollection(QueryBatch queryBatch)
        {
            QueryBatch = queryBatch;
        }
    }
}
