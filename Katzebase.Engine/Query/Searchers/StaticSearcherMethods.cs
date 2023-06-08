using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Searchers.MultiSchema.Intersection;
using Katzebase.Engine.Query.Searchers.MultiSchema.Mapping;
using Katzebase.Engine.Query.Searchers.SingleSchema;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Query.Searchers
{
    internal class StaticSearcherMethods
    {
        /// <summary>
        /// Finds all document using a prepared query.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="KbInvalidSchemaException"></exception>
        internal static KbQueryResult FindDocumentsByPreparedQuery(Core core, PerformanceTrace? pt, Transaction transaction, PreparedQuery query)
        {
            var result = new KbQueryResult();

            if (query.SelectFields.Count == 1 && query.SelectFields[0].Key == "*")
            {
                query.SelectFields.Clear();
                throw new KbNotImplementedException("Select * is not implemented. This will require schema sampling.");
            }
            else if (query.SelectFields.Count == 0)
            {
                query.SelectFields.Clear();
                throw new KbGenericException("No fields were selected.");
            }

            if (query.Schemas.Count == 0)
            {
                //I'm not even sure we can get here. Thats an exception, right?
                throw new KbGenericException("No schemas were selected.");
            }
            //If we are querying a single schema, then we just have to apply the conditions in a few threads. Hand off the request and make it so.
            else if (query.Schemas.Count == 1)
            {
                var singleSchema = query.Schemas.First();

                var subsetResults = SSQStaticMethods.GetSingleSchemaDocumentsByConditions(core, pt, transaction, singleSchema.Name, query);

                foreach (var field in query.SelectFields)
                {
                    result.Fields.Add(new KbQueryField(field.Alias));
                }

                foreach (var subsetResult in subsetResults.Collection)
                {
                    result.Rows.Add(new KbQueryRow(subsetResult.Values));
                }
            }
            //If we are querying multiple schemas then we have to intersect the schemas and apply the conditions. Oh boy.
            else if (query.Schemas.Count > 1)
            {
                var schemaMap = new MSQQuerySchemaMap();

                foreach (var querySchema in query.Schemas)
                {
                    //Lock the schema:
                    var ptLockSchema = pt?.BeginTrace<PersistSchema>(PerformanceTraceType.Lock);
                    var schemaMeta = core.Schemas.VirtualPathToMeta(transaction, querySchema.Name, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(querySchema.Name);
                    }
                    ptLockSchema?.EndTrace();
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    //Lock the document catalog:
                    var documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);
                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(pt, transaction, documentCatalogDiskPath, LockOperation.Read);
                    Utility.EnsureNotNull(documentCatalog);

                    schemaMap.Add(querySchema.Alias, schemaMeta, documentCatalog, querySchema.Conditions);
                }

                //Figure out which indexes could assist us in retrieving the desired documents (if any).
                var ptOptimization = pt?.BeginTrace(PerformanceTraceType.Optimization);
                var lookupOptimization = SSQStaticOptimization.SelectIndexesForConditionLookupOptimization(core, transaction, schemaMap.First().Value.SchemaMeta, query.Conditions);
                ptOptimization?.EndTrace();

                /*
                 *  We need to build a generic key/value dataset which is the combined fieldset from each inner joined document.
                 *  Then we use the conditions that were supplied to eliminate results from that dataset.
                */

                var schemaMapResults = MSQStaticSchemaJoiner.IntersetSchemas(core, pt, transaction, schemaMap, query, lookupOptimization);

                HashSet<string> strings = new HashSet<string>();
            }

            return result;
        }

    }
}
