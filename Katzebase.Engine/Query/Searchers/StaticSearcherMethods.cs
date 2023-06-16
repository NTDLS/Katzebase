using Katzebase.Engine.Query.Searchers.MultiSchema;
using Katzebase.Engine.Query.Searchers.MultiSchema.Mapping;
using Katzebase.Engine.Query.Searchers.SingleSchema;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

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

            //Lock the schema:
            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);

            //Open the document page catalog:
            var documentPageCatalog = core.Documents.GetDocumentPageCatalog(transaction, physicalSchema, LockOperation.Write);

            if (documentPageCatalog.PageMappings.Count > 0)
            {
                Random random = new Random(Environment.TickCount);

                for (int i = 0; i < rowLimit || rowLimit == 0; i++)
                {
                    int pageNumber = random.Next(0, documentPageCatalog.PageMappings.Count - 1);
                    var pageMap = documentPageCatalog.PageMappings[pageNumber];

                    int documentIndex = random.Next(0, pageMap.DocumentIDs.Count - 1);
                    var documentId = pageMap.DocumentIDs.ToArray()[documentIndex];
                    var physicalDocument = core.Documents.GetDocument(transaction, physicalSchema, documentId, LockOperation.Read);

                    var jContent = JObject.Parse(physicalDocument.Content);

                    if (i == 0)
                    {
                        foreach (var jToken in jContent)
                        {
                            result.Fields.Add(new KbQueryField(jToken.Key));
                        }
                    }

                    if (rowLimit == 0) //We just want a field list.
                    {
                        break;
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

            //Lock the schema:
            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);

            //Lock the document catalog:
            var documentCatalog = core.Documents.GetPageDocuments(transaction, physicalSchema, LockOperation.Read).ToList();

            for (int i = 0; i < documentCatalog.Count && (i < topCount || topCount < 0); i++)
            {
                var pageDocuent = documentCatalog[i];

                var persistDocument = core.Documents.GetDocument(transaction, physicalSchema, pageDocuent.DocumentId, LockOperation.Read);

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

            if (query.SelectFields.Count == 1 && query.SelectFields[0].Field == "*")
            {
                query.SelectFields.Clear();

                var ptSample = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Sampling);
                foreach (var schema in query.Schemas)
                {
                    var sample = SampleSchemaDocuments(core, transaction, schema.Name, 0);

                    foreach (var field in sample.Fields)
                    {
                        if (schema.Prefix != string.Empty)
                        {
                            query.SelectFields.Add(schema.Prefix, field.Name, $"{schema.Prefix}.{field.Name}");
                        }
                        else
                        {
                            query.SelectFields.Add($"{field.Name}");
                        }
                    }
                }
                ptSample?.StopAndAccumulate();
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
                //-------------------------------------------------------------------------------------------------------------
                //This is where we do SSQ stuff (Single Schema Query), e.g. queried with NO joins.
                //-------------------------------------------------------------------------------------------------------------
                var singleSchema = query.Schemas.First();

                var subsetResults = SSQStaticMethods.GetDocumentsByConditions(core, transaction, singleSchema.Name, query);

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
                    //Lock the schema:
                    var physicalSchema = core.Schemas.Acquire(transaction, querySchema.Name, LockOperation.Read);

                    //Lock the document catalog:
                    var physicalDocumentPageCatalog = core.Documents.GetDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

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
