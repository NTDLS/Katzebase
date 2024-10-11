using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.PersistentTypes.Document;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class StaticSearcherProcessor
    {
        /// <summary>
        /// Returns a random sample of all document fields from a schema.
        /// </summary>
        internal static KbQueryResult SampleSchemaDocuments(
            EngineCore core, Transaction transaction, string schemaName, int rowLimit = -1)
        {
            var result = new KbQueryResult();

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
        internal static KbQueryResult ListSchemaDocuments(EngineCore core, Transaction transaction, string schemaName, int topCount = -1)
        {
            var result = new KbQueryResult();

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
        internal static KbQueryResult FindDocumentsByQuery(EngineCore core, Transaction transaction, Query query)
        {
            var schemaMap = new QuerySchemaMap(core, transaction, query);

            foreach (var querySchema in query.Schemas)
            {
                var physicalSchema = core.Schemas.Acquire(transaction, querySchema.Name, LockOperation.Read);
                var physicalDocumentPageCatalog = core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

                schemaMap.Add(querySchema.Alias, physicalSchema, querySchema.SchemaUsageType, physicalDocumentPageCatalog, querySchema.Conditions);
            }

            var lookupResults = StaticSchemaIntersectionProcessor.GetDocumentsByConditions(core, transaction, schemaMap, query);

            var result = new KbQueryResult();

            foreach (var field in query.SelectFields)
            {
                result.Fields.Add(new KbQueryField(field.Alias));
            }

            foreach (var row in lookupResults.Rows)
            {
                result.Rows.Add(new KbQueryRow(row.Values));
            }

            return result;
        }

        public class SchemaIntersectionRowDocumentIdentifierCollection
            : Dictionary<DocumentPointer, KbInsensitiveDictionary<KbInsensitiveDictionary<string?>>>
        {

        }

        /// <summary>
        /// Executes a prepared query (select, update, delete, etc) and returns
        ///     just the distinct document pointers for the specified schema.
        /// </summary>
        internal static SchemaIntersectionRowDocumentIdentifierCollection FindDocumentPointersByQuery(
            EngineCore core, Transaction transaction, Query query, List<string> gatherDocumentPointersForSchemaAliases)
        {
            var schemaMap = new QuerySchemaMap(core, transaction, query);

            foreach (var querySchema in query.Schemas)
            {
                var physicalSchema = core.Schemas.Acquire(transaction, querySchema.Name, LockOperation.Read);
                var physicalDocumentPageCatalog = core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

                schemaMap.Add(querySchema.Alias, physicalSchema, querySchema.SchemaUsageType, physicalDocumentPageCatalog, querySchema.Conditions);
            }

            var schemaIntersectionRowCollection = StaticSchemaIntersectionProcessor.GatherIntersectedRows(
                core, transaction, schemaMap, query, gatherDocumentPointersForSchemaAliases);

            var schemaIntersectionRowDocumentIdentifierCollection = new SchemaIntersectionRowDocumentIdentifierCollection();

            foreach (var schemaIntersectionRow in schemaIntersectionRowCollection)
            {
                //Get the document pointers for the given schemas. I do not believe that this additional filtering is required,
                //  but this function is used for UPDATE and DELETE statements so maybe the extra cycles are warranted?
                foreach (var documentPointer in schemaIntersectionRow.DocumentPointers
                    .Where(o => gatherDocumentPointersForSchemaAliases.Contains(o.Key, StringComparer.InvariantCultureIgnoreCase)))
                {
                    //In the case of a cartesian expression, there can be multiple instances of the same document pointer we "upsert" the collection.
                    schemaIntersectionRowDocumentIdentifierCollection[documentPointer.Value] = schemaIntersectionRow.SchemaElements;
                }
            }

            return schemaIntersectionRowDocumentIdentifierCollection;
        }
    }
}
