using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Schemas;

namespace Katzebase.Engine.Documents.Query.MultiSchema.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// </summary>
    public class MSQQuerySchemaMapItem
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
