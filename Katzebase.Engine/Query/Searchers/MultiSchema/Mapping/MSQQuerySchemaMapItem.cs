using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;

namespace Katzebase.Engine.Query.Searchers.MultiSchema.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// </summary>
    internal class MSQQuerySchemaMapItem
    {
        public PersistSchema SchemaMeta { get; set; }
        public PersistDocumentCatalog DocuemntCatalog { get; set; }
        public Conditions? Conditions { get; set; }

        public MSQQuerySchemaMapItem(PersistSchema schemaMeta, PersistDocumentCatalog docuemntCatalog, Conditions? conditions)
        {
            SchemaMeta = schemaMeta;
            DocuemntCatalog = docuemntCatalog;
            Conditions = conditions;
        }
    }
}
