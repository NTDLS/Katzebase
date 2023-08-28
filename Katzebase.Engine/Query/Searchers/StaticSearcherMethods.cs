﻿using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Searchers.Mapping;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Query.Searchers
{
    internal class StaticSearcherMethods
    {
        /// <summary>
        /// Returns a random sample of all document fields from a schema.
        /// </summary>
        internal static KbQueryResult SampleSchemaDocuments(Core core, Transaction transaction, string schemaName, int rowLimit = -1)
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
                    var physicalDocumentPageMap = core.Documents.AcquireDocumentPageMap(transaction, physicalSchema, pageNumber, LockOperation.Read);
                    var documentId = physicalDocumentPageMap.DocumentIDs.ToArray()[documentIndex];
                    var physicalDocument = core.Documents.AcquireDocument(transaction, physicalSchema, new DocumentPointer(pageNumber, documentId), LockOperation.Read);

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
                        physicalDocument.Elements.TryGetValue(field.Name, out string? jToken);
                        resultRow.AddValue(jToken?.ToString() ?? string.Empty);
                    }

                    result.Rows.Add(resultRow);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a top list of all document fields from a schema.
        /// </summary>
        internal static KbQueryResult ListSchemaDocuments(Core core, Transaction transaction, string schemaName, int topCount)
        {
            var result = new KbQueryResult();

            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
            var documentPointers = core.Documents.AcquireDocumentPointers(transaction, physicalSchema, LockOperation.Read).ToList();

            for (int i = 0; i < documentPointers.Count && (i < topCount || topCount < 0); i++)
            {
                var pageDocuent = documentPointers[i];

                var persistDocument = core.Documents.AcquireDocument(transaction, physicalSchema, pageDocuent, LockOperation.Read);

                if (i == 0)
                {
                    foreach (var jToken in persistDocument.Elements)
                    {
                        result.Fields.Add(new KbQueryField(jToken.Key));
                    }
                }

                var resultRow = new KbQueryRow();
                foreach (var field in result.Fields)
                {
                    persistDocument.Elements.TryGetValue(field.Name, out string? jToken);
                    resultRow.AddValue(jToken?.ToString() ?? string.Empty);
                }

                result.Rows.Add(resultRow);
            }

            return result;
        }

        /// <summary>
        /// Finds all documents using a prepared query. Performs all filtering and ordering.
        /// </summary>
        internal static KbQueryResult FindDocumentsByPreparedQuery(Core core, Transaction transaction, PreparedQuery query)
        {
            var result = new KbQueryResult();

            var schemaMap = new QuerySchemaMap(core, transaction);

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

            var subsetResults = StaticSchemaIntersectionMethods.GetDocumentsByConditions(core, transaction, schemaMap, query);

            foreach (var field in query.SelectFields)
            {
                result.Fields.Add(new KbQueryField(field.Alias));
            }

            foreach (var subsetResult in subsetResults.Collection)
            {
                result.Rows.Add(new KbQueryRow(subsetResult.Values));
            }

            return result;
        }

        /// <summary>
        /// Executes a prepared query (select, update, delete, etc) and returns just the distinct document pointers for the specified schema.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="transaction"></param>
        /// <param name="query"></param>
        /// <param name="schemaPrefix"></param>
        /// <returns></returns>
        internal static IEnumerable<DocumentPointer> FindDocumentPointersByPreparedQuery(Core core, Transaction transaction, PreparedQuery query, string schemaPrefix)
        {
            var schemaMap = new QuerySchemaMap(core, transaction);

            foreach (var querySchema in query.Schemas)
            {
                var physicalSchema = core.Schemas.Acquire(transaction, querySchema.Name, LockOperation.Read);
                var physicalDocumentPageCatalog = core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

                schemaMap.Add(querySchema.Prefix, physicalSchema, physicalDocumentPageCatalog, querySchema.Conditions);
            }

            var subsetResults = StaticSchemaIntersectionMethods.GetDocumentsByConditions(core, transaction, schemaMap, query, schemaPrefix);
            return subsetResults.DocumentPointers;
        }

    }
}
