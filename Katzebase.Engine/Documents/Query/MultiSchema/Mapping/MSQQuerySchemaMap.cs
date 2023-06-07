using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Schemas;

namespace Katzebase.Engine.Documents.Query.MultiSchema.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// The key to the dictonary is the schema alias.
    /// </summary>
    public class MSQQuerySchemaMap : Dictionary<string, MSQQuerySchemaMapItem>
    {
        /// <summary>
        /// Adds a mapping to the schema mapping collection/
        /// </summary>
        /// <param name="key">The alias of the schema</param>
        /// <param name="schemaMeta">The associated schema meta-data.</param>
        /// <param name="docuemntCatalog">The document catalog contained in the associated schema.</param>
        /// <param name="conditions">The conditons used to join this schema mapping to the one before it.</param>
        public void Add(string key, PersistSchema schemaMeta, PersistDocumentCatalog docuemntCatalog, Conditions? conditions)
        {
            Add(key, new MSQQuerySchemaMapItem(schemaMeta, docuemntCatalog, conditions));
        }
    }
}
