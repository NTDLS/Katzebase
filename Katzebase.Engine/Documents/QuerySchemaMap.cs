using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Schemas;
using static Katzebase.Engine.Documents.DocumentManager;

namespace Katzebase.Engine.Documents
{
    public class QuerySchemaMap : Dictionary<String, QuerySchemaMapItem>
    {
        public void Add(string key, PersistSchema schemaMeta, PersistDocumentCatalog docuemntCatalog, Conditions? conditions)
        {
            this.Add(key, new QuerySchemaMapItem(schemaMeta, docuemntCatalog, conditions));
        }
    }
}
