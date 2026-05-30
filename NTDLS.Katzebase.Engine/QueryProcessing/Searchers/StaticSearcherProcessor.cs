using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping;
using NTDLS.Katzebase.Parsers;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class StaticSearcherProcessor
    {
        /// <summary>
        /// Returns a random sample of all document fields from a schema.
        /// </summary>
        internal static KbQueryResult SampleSchemaFields(
            EngineCore core, Transaction transaction, string schemaName)
        {

            var result = new KbQueryResult();

            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
            var currentIdentity = core.Documents.GetCurrentIdentity(physicalSchema);

            var rdb = core.IO.AcquireDocumentsRdb(physicalSchema);

            if (currentIdentity > 0)
            {
                int unsuccessfulAttempts = 0;

                while (unsuccessfulAttempts < 10)
                {
                    uint documentId = (uint)Random.Shared.NextInt64(0, currentIdentity + 1);

                    var physicalDocument = core.Documents.AcquireDocumentVirtual(transaction, rdb, documentId, LockOperation.Read);

                    if (physicalDocument == null)
                    {
                        unsuccessfulAttempts++;
                        continue;
                    }

                    if (result.Fields.Count == 0)
                    {
                        foreach (var documentValue in physicalDocument.Elements)
                        {
                            result.Fields.Add(new KbQueryField(documentValue.Key));
                        }
                    }

                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a random sample of all document fields from a schema.
        /// </summary>
        internal static KbQueryResult SampleSchemaDocuments(
            EngineCore core, Transaction transaction, string schemaName, int rowLimit = -1)
        {
            var result = new KbQueryResult();

            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
            var currentIdentity = core.Documents.GetCurrentIdentity(physicalSchema);
            var rdb = core.IO.AcquireDocumentsRdb(physicalSchema);

            if (currentIdentity > 0)
            {
                int unsuccessfulAttempts = 0;

                while (result.Rows.Count < rowLimit && unsuccessfulAttempts < 10)
                {
                    uint documentId = (uint)Random.Shared.NextInt64(0, currentIdentity + 1);

                    var physicalDocument = core.Documents.AcquireDocumentVirtual(transaction, rdb, documentId, LockOperation.Read);

                    if (physicalDocument == null)
                    {
                        unsuccessfulAttempts++;
                        continue;
                    }

                    if (result.Fields.Count == 0)
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
        internal static KbQueryResult ListSchemaDocuments(EngineCore core, Transaction transaction, string schemaName, int? topCount = null)
        {
            var result = new KbQueryResult();

            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
            var rdb = core.IO.AcquireDocumentsRdb(physicalSchema);
            var documentId = core.Documents.AcquireDocumentPointers(transaction, physicalSchema, LockOperation.Read, topCount).ToList();

            for (int i = 0; i < documentId.Count && (i < topCount || topCount < 0); i++)
            {
                var persistDocument = core.Documents.AcquireDocument(transaction, rdb, documentId[i], LockOperation.Read);

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
        internal static KbQueryResult FindDocumentsByQuery(EngineCore core, Transaction transaction, PreparedQuery query)
        {
            var schemaMap = new QuerySchemaOptimizationMap(core, transaction, query);

            foreach (var querySchema in query.Schemas)
            {
                var physicalSchema = core.Schemas.Acquire(transaction, querySchema.Name, LockOperation.Read);

                var querySchemaMapItem = new QuerySchemaOptimizationMapItem(core, transaction, schemaMap, physicalSchema, querySchema.SchemaUsageType, querySchema.Conditions, querySchema.Alias);
                schemaMap.Add(querySchema.Alias, querySchemaMapItem);
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
            : Dictionary<uint, KbInsensitiveDictionary<KbInsensitiveDictionary<string?>>>
        {

        }

        /// <summary>
        /// Executes a prepared query (select, update, delete, etc) and returns
        ///     just the distinct document pointers for the specified schema.
        /// </summary>
        internal static SchemaIntersectionRowDocumentIdentifierCollection FindDocumentPointersByQuery(
            EngineCore core, Transaction transaction, PreparedQuery query, List<string> gatherDocumentPointersForSchemaAliases)
        {
            var schemaMap = new QuerySchemaOptimizationMap(core, transaction, query);

            foreach (var querySchema in query.Schemas)
            {
                var physicalSchema = core.Schemas.Acquire(transaction, querySchema.Name, LockOperation.Read);

                var querySchemaMapItem = new QuerySchemaOptimizationMapItem(core, transaction, schemaMap, physicalSchema, querySchema.SchemaUsageType, querySchema.Conditions, querySchema.Alias);
                schemaMap.Add(querySchema.Alias, querySchemaMapItem);
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
