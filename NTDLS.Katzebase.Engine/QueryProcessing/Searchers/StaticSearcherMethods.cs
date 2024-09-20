using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class StaticSearcherMethods
    {
        /// <summary>
        /// Returns a random sample of all document fields from a schema.
        /// </summary>
        internal static KbQueryDocumentListResult SampleSchemaDocuments(
            EngineCore core, Transaction transaction, string schemaName, int rowLimit = -1)
        {
            var result = new KbQueryDocumentListResult();

            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
            var physicalDocumentPageCatalog = core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

            if (physicalDocumentPageCatalog.Catalog.Count > 0)
            {
                var random = new Random(Environment.TickCount);

                for (int i = 0; i < rowLimit; i++)
                {
                    int pageNumber = random.Next(0, physicalDocumentPageCatalog.Catalog.Count - 1);
                    var pageCatalog = physicalDocumentPageCatalog.Catalog[pageNumber];
                    int documentIndex = random.Next(0, pageCatalog.DocumentCount - 1);

                    var physicalDocumentPageMap = core.Documents.AcquireDocumentPageMap(
                        transaction, physicalSchema, pageNumber, LockOperation.Read);

                    var documentId = physicalDocumentPageMap.DocumentIDs.ToArray()[documentIndex];

                    var physicalDocument = core.Documents.AcquireDocument(
                        transaction, physicalSchema, new DocumentPointer(pageNumber, documentId), LockOperation.Read);

                    if (i == 0)
                    {
                        foreach (var documentValue in physicalDocument.Elements)
                        {
                            result.Fields.Add(new KbQueryField(documentValue.Key));
                        }
                    }

                    var resultRow = new KbQueryRow();
                    resultRow.AddValue(documentId.ToString());

                    foreach (var field in result.Fields.Skip(1))
                    {
                        physicalDocument.Elements.TryGetValue(field.Name, out string? element);
                        resultRow.AddValue(element?.ToString() ?? string.Empty);
                    }

                    result.Rows.Add(resultRow);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a top list of all document fields from a schema.
        /// </summary>
        internal static KbQueryDocumentListResult ListSchemaDocuments(EngineCore core, Transaction transaction, string schemaName, int topCount = -1)
        {
            var result = new KbQueryDocumentListResult();

            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
            var documentPointers = core.Documents.AcquireDocumentPointers(transaction, physicalSchema, LockOperation.Read, topCount).ToList();

            for (int i = 0; i < documentPointers.Count && (i < topCount || topCount < 0); i++)
            {
                var pageDocument = documentPointers[i];

                var persistDocument = core.Documents.AcquireDocument(transaction, physicalSchema, pageDocument, LockOperation.Read);

                if (i == 0)
                {
                    foreach (var element in persistDocument.Elements)
                    {
                        result.Fields.Add(new KbQueryField(element.Key));
                    }
                }

                var resultRow = new KbQueryRow();
                foreach (var field in result.Fields)
                {
                    persistDocument.Elements.TryGetValue(field.Name, out string? element);
                    resultRow.AddValue(element?.ToString() ?? string.Empty);
                }

                result.Rows.Add(resultRow);
            }

            return result;
        }

        /// <summary>
        /// Finds all documents using a prepared query. Performs all filtering and ordering.
        /// </summary>
        internal static KbQueryDocumentListResult FindDocumentsByPreparedQuery(EngineCore core, Transaction transaction, PreparedQuery query)
        {
            var result = new KbQueryDocumentListResult();

            var schemaMap = new QuerySchemaMap(core, transaction, query);

            foreach (var querySchema in query.Schemas)
            {
                var physicalSchema = core.Schemas.Acquire(transaction, querySchema.Name, LockOperation.Read);
                var physicalDocumentPageCatalog = core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

                schemaMap.Add(querySchema.Prefix, physicalSchema, physicalDocumentPageCatalog, querySchema.Conditions);
            }

            /*
             *  We need to build a generic key/value dataset which is the combined field-set from each inner joined document.
             *  Then we use the conditions that were supplied to eliminate results from that dataset.
            */

            var subConditionResults = StaticSchemaIntersectionMethods.GetDocumentsByConditions(core, transaction, schemaMap, query);

            foreach (var field in query.SelectFields)
            {
                result.Fields.Add(new KbQueryField(field.Alias));
            }

            foreach (var subConditionResult in subConditionResults.RowValues)
            {
                result.Rows.Add(new KbQueryRow(subConditionResult));
            }

            return result;
        }

        /// <summary>
        /// Executes a prepared query (select, update, delete, etc) and returns
        ///     just the distinct document pointers for the specified schema.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="transaction"></param>
        /// <param name="query"></param>
        /// <param name="schemaPrefix"></param>
        /// <returns></returns>
        internal static IEnumerable<SchemaIntersectionRowDocumentIdentifier> FindDocumentPointersByPreparedQuery(
            EngineCore core, Transaction transaction, PreparedQuery query, string[] getDocumentsIdsForSchemaPrefixes)
        {
            var schemaMap = new QuerySchemaMap(core, transaction, query);

            foreach (var querySchema in query.Schemas)
            {
                var physicalSchema = core.Schemas.Acquire(transaction, querySchema.Name, LockOperation.Read);
                var physicalDocumentPageCatalog = core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

                schemaMap.Add(querySchema.Prefix, physicalSchema, physicalDocumentPageCatalog, querySchema.Conditions);
            }

            return StaticSchemaIntersectionMethods.GetDocumentsByConditions(core, transaction, schemaMap, query, getDocumentsIdsForSchemaPrefixes).RowDocumentIdentifiers;
        }
    }
}
