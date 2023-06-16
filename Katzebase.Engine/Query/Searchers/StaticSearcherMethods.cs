using Katzebase.Engine.Query.Searchers.MultiSchema;
using Katzebase.Engine.Query.Searchers.MultiSchema.Mapping;
using Katzebase.Engine.Query.Searchers.SingleSchema;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Query.Searchers
{
    internal class StaticSearcherMethods
    {
        /// <summary>
        /// Returns a random sample of all docuemnt fields from a schema.
        /// </summary>
        internal static KbQueryResult SampleSchemaDocuments(Core core, Transaction transaction, string schemaName, int rowLimit = -1)
        {
            var result = new KbQueryResult();

            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
            var documentPageCatalog = core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Write);

            if (documentPageCatalog.PageMappings.Count > 0)
            {
                var random = new Random(Environment.TickCount);

                for (int i = 0; i < rowLimit; i++)
                {
                    int pageNumber = random.Next(0, documentPageCatalog.PageMappings.Count - 1);
                    var pageMap = documentPageCatalog.PageMappings[pageNumber];

                    int documentIndex = random.Next(0, pageMap.DocumentIDs.Count - 1);
                    var documentId = pageMap.DocumentIDs.ToArray()[documentIndex];
                    var physicalDocument = core.Documents.AcquireDocument(transaction, physicalSchema, documentId, LockOperation.Read);

                    var jContent = JObject.Parse(physicalDocument.Content);

                    if (i == 0)
                    {
                        foreach (var jToken in jContent)
                        {
                            result.Fields.Add(new KbQueryField(jToken.Key));
                        }
                    }

                    var resultRow = new KbQueryRow();
                    resultRow.AddValue(physicalDocument.Id.ToString());

                    foreach (var field in result.Fields.Skip(1))
                    {
                        jContent.TryGetValue(field.Name, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken);
                        resultRow.AddValue(jToken?.ToString() ?? string.Empty);
                    }

                    result.Rows.Add(resultRow);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a top list of all docuemnt fields from a schema.
        /// </summary>
        internal static KbQueryResult ListSchemaDocuments(Core core, Transaction transaction, string schemaName, int topCount)
        {
            var result = new KbQueryResult();

            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
            var documentPointers = core.Documents.AcquireDocumentPointers(transaction, physicalSchema, LockOperation.Read).ToList();

            for (int i = 0; i < documentPointers.Count && (i < topCount || topCount < 0); i++)
            {
                var pageDocuent = documentPointers[i];

                var persistDocument = core.Documents.AcquireDocument(transaction, physicalSchema, pageDocuent.DocumentId, LockOperation.Read);

                var jContent = JObject.Parse(persistDocument.Content);

                if (i == 0)
                {
                    foreach (var jToken in jContent)
                    {
                        result.Fields.Add(new KbQueryField(jToken.Key));
                    }
                }

                var resultRow = new KbQueryRow();
                foreach (var field in result.Fields)
                {
                    jContent.TryGetValue(field.Name, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken);
                    resultRow.AddValue(jToken?.ToString() ?? string.Empty);
                }

                result.Rows.Add(resultRow);
            }

            return result;
        }

        /// <summary>
        /// Finds all documents using a prepared query.
        /// </summary>
        internal static KbQueryResult FindDocumentsByPreparedQuery(Core core, Transaction transaction, PreparedQuery query)
        {
            var result = new KbQueryResult();

            //If we are querying a single schema, then we just have to apply the conditions in a few threads. Hand off the request and make it so.
            if (query.Schemas.Count == 1)
            {
                //-------------------------------------------------------------------------------------------------------------
                //This is where we do SSQ stuff (Single Schema Query), e.g. queried with NO joins.
                //-------------------------------------------------------------------------------------------------------------
                var singleSchema = query.Schemas.First();

                var subsetResults = SSQStaticMethods.GetDocumentsByConditions(core, transaction, singleSchema, query);

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
                //-------------------------------------------------------------------------------------------------------------
                //This is where we do MSQ stuff (Multi Schema Query), e.g. queried WITH joins.
                //-------------------------------------------------------------------------------------------------------------

                var schemaMap = new MSQQuerySchemaMap(core, transaction);

                foreach (var querySchema in query.Schemas)
                {
                    var physicalSchema = core.Schemas.Acquire(transaction, querySchema.Name, LockOperation.Read);
                    var physicalDocumentPageCatalog = core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

                    schemaMap.Add(querySchema.Prefix, physicalSchema, physicalDocumentPageCatalog, querySchema.Conditions);
                }

                /*
                 *  We need to build a generic key/value dataset which is the combined fieldset from each inner joined document.
                 *  Then we use the conditions that were supplied to eliminate results from that dataset.
                */

                var subsetResults = MSQStaticMethods.GetDocumentsByConditions(core, transaction, schemaMap, query);

                foreach (var field in query.SelectFields)
                {
                    result.Fields.Add(new KbQueryField(field.Alias));
                }

                foreach (var subsetResult in subsetResults.Collection)
                {
                    result.Rows.Add(new KbQueryRow(subsetResult.Values));
                }
            }

            return result;
        }

    }
}
