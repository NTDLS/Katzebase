using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Schemas;

namespace Katzebase.Engine.Documents
{
    public class QuerySchemaMapItem
    {
        public PersistSchema SchemaMeta { get; set; }
        public PersistDocumentCatalog DocuemntCatalog { get; set; }
        public Conditions? Conditions { get; set; }

        public QuerySchemaMapItem(PersistSchema schemaMeta, PersistDocumentCatalog docuemntCatalog, Conditions? conditions)
        {
            SchemaMeta = schemaMeta;
            DocuemntCatalog = docuemntCatalog;
            Conditions = conditions;
        }
    }
}
