using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Transactions;
using Katzebase.Library;
using Katzebase.Library.Exceptions;
using Katzebase.Library.Payloads;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Documents
{
    public class DocumentManager
    {
        private Core core;

        public DocumentManager(Core core)
        {
            this.core = core;
        }

        public KbQueryResult ExecuteSelect(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult(); ;

                using (var transaction = core.Transactions.Begin(processId))
                {
                    result = FindDocuments(transaction.Transaction, preparedQuery);
                    transaction.Commit();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete document by ID for process {processId}.", ex);
                throw;
            }
        }

        public KbActionResponse ExecuteDelete(ulong processId, PreparedQuery preparedQuery)
        {
            //TODO: This is a stub, this does NOT work.
            try
            {
                var result = new KbActionResponse();

                using (var transaction = core.Transactions.Begin(processId))
                {
                    result = FindDocuments(transaction.Transaction, preparedQuery);

                    //TODO: Delete the documents.

                    /*
                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);

                    var persistDocument = documentCatalog.GetById(newId);
                    if (persistDocument != null)
                    {
                        string documentDiskPath = Path.Combine(schemaMeta.DiskPath, Helpers.GetDocumentModFilePath(persistDocument.Id));

                        core.IO.DeleteFile(txRef.Transaction, documentDiskPath);

                        documentCatalog.Remove(persistDocument);

                        core.Indexes.DeleteDocumentFromIndexes(txRef.Transaction, schemaMeta, persistDocument.Id);

                        core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);
                    }
                    */

                    transaction.Commit();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete document by ID for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets all documents by a subset of conditions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="documentCatalog"></param>
        /// <param name="schemaMeta"></param>
        /// <param name="query"></param>
        /// <param name="conditionSubset"></param>
        /// <returns></returns>
        /// <exception cref="KbParserException"></exception>
        private DocumentLookupResults GetDocumentsByConditionSubset(Transaction transaction,
            PersistDocumentCatalog documentCatalog, PersistSchema schemaMeta, PreparedQuery query, ConditionSubset conditionSubset)
        {
            var results = new DocumentLookupResults();

            //Loop through each document in the catalog:
            foreach (var item in documentCatalog.Collection)
            {
                Utility.EnsureNotNull(schemaMeta.DiskPath);
                var persistDocumentDiskPath = Path.Combine(schemaMeta.DiskPath, item.FileName);

                var persistDocument = core.IO.GetJson<PersistDocument>(transaction, persistDocumentDiskPath, LockOperation.Read);
                Utility.EnsureNotNull(persistDocument);
                Utility.EnsureNotNull(persistDocument.Content);

                var jContent = JObject.Parse(persistDocument.Content);

                var expression = new StringBuilder();

                //Loop though each condition in the prepared query and build an expression to see if the document meets the criteria
                //  by building a logical expression that we can evaluate 
                foreach (var condition in conditionSubset.Conditions.OfType<ConditionSingle>())
                {
                    Utility.EnsureNotNull(condition.Left.Value); //TODO: What do we really need to do here?

                    //Get the value of the condition:
                    if (jContent.TryGetValue(condition.Left.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                    {
                        if (expression.Length > 0)
                        {
                            expression.Append(condition.LogicalConnector == LogicalConnector.And ? "&&" : "||");
                        }

                        if (condition.IsMatch(jToken.ToString().ToLower()))
                        {
                            expression.Append($"(1)");
                        }
                        else
                        {
                            expression.Append($"(0)");
                        }
                    }
                    else
                    {
                        throw new KbParserException($"Field not found in document [{condition.Left.Value}].");
                    }
                }

                var expressionResult = (bool)(new NCalc.Expression(expression.ToString())).Evaluate();

                if (expressionResult)
                {
                    Utility.EnsureNotNull(persistDocument.Id);
                    var result = new DocumentLookupResult((Guid)persistDocument.Id);

                    foreach (string field in query.SelectFields)
                    {
                        if (jContent.TryGetValue(field, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                        {
                            result.Values.Add(jToken.ToString());
                        }
                        else
                        {
                            result.Values.Add(string.Empty);
                        }
                    }

                    results.Add(result);
                }
            }

            return results;
        }

        private KbQueryResult FindDocuments(Transaction transaction, PreparedQuery query)
        {
            var result = new KbQueryResult();

            //Lock the schema:
            var schemaMeta = core.Schemas.VirtualPathToMeta(transaction, query.Schema, LockOperation.Read);
            if (schemaMeta == null || schemaMeta.Exists == false)
            {
                throw new KbSchemaDoesNotExistException(query.Schema);
            }
            Utility.EnsureNotNull(schemaMeta.DiskPath);

            //Lock the document catalog:
            var documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, Constants.DocumentCatalogFile);
            var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(transaction, documentCatalogDiskPath, LockOperation.Read);
            Utility.EnsureNotNull(documentCatalog);

            foreach (var field in query.SelectFields)
            {
                result.Fields.Add(new KbQueryField(field));
            }

            //Figure out which indexes could assist us in retrieving the desired documents (if any).
            var lookupOptimization = core.Indexes.SelectIndexesForConditionLookupOptimization(transaction, schemaMeta, query.Conditions);

            string subsetExpressionTree = query.Conditions.BuildSubsetExpressionTree();

            var allResultRIDs = new HashSet<Guid>();
            var allResults = new List<DocumentLookupLogicSubsetResult>();

            foreach (var conditionGroup in lookupOptimization.FlatConditionGroups)
            {
                var subset = conditionGroup.ToSubset();

                var subsetResults = GetDocumentsByConditionSubset(transaction, documentCatalog, schemaMeta, query, subset);

                allResults.Add(new DocumentLookupLogicSubsetResult(subset.SubsetUID, subset.LogicalConnector, subsetResults));

                //Save a big list of all unique RIDs (Row IDs) so we can loop through them later an elimitate any that dont match all condition subsets.
                var currentRIDs = subsetResults.Collection.Select(o => o.RID).ToHashSet();
                allResultRIDs.UnionWith(currentRIDs);
            }

            var expression = new NCalc.Expression(subsetExpressionTree);

            //Loop through ALL found document IDs and build an expression for each one that represents all condition subsets.
            //  This way we can eliminate documents that do not match all condition subsets.
            foreach (var rid in allResultRIDs)
            {
                DocumentLookupResult? workingDocument = null;

                //Build a logical expression for each condition subset.
                foreach (var conditionGroup in lookupOptimization.FlatConditionGroups)
                {
                    var resultSet = allResults.Where(o => o.SubsetUID == conditionGroup.SubsetUID).First();
                    var resultSetDocument = resultSet.Results.Collection.FirstOrDefault(o => o.RID == rid);

                    workingDocument ??= resultSetDocument; //Save the first instance of the document we found. This will be used for the final result.

                    //The expression is parameterized, so add a true/false parameter for each conditon which defines whether
                    //  the document was found in the given condition subset.
                    expression.Parameters[conditionGroup.SubsetVariableName] = (resultSetDocument != null);
                }

                Utility.EnsureNotNull(workingDocument); //We absolutley expect to have a document here, if not - something is terribly wrong.

                //Evaluate the expression and if its true, save the document results for the final result.
                var expressionResult = (bool)expression.Evaluate();
                if (expressionResult)
                {
                    result.Rows.Add(new KbQueryRow(workingDocument.Values));
                }
            }

            return result;

            /*
            if (lookupOptimization.IndexSelection.Count == 0) //Full schema scan. Ouch!
            {
                //Loop through each document in the catalog:
                foreach (var item in documentCatalog.Collection)
                {
                    var persistDocumentDiskPath = Path.Combine(schemaMeta.DiskPath, item.FileName);

                    var persistDocument = core.IO.GetJson<PersistDocument>(transaction, persistDocumentDiskPath, LockOperation.Read);
                    Utility.EnsureNotNull(persistDocument);
                    Utility.EnsureNotNull(persistDocument.Content);

                    var jContent = JObject.Parse(persistDocument.Content);

                    if (query.Conditions.IsMatch(jContent))
                    {
                        var rowValues = new List<string>();

                        foreach (string field in query.SelectFields)
                        {
                            if (field == "#RID")
                            {
                                rowValues.Add((persistDocument.Id ?? Guid.Empty).ToString());
                            }
                            else
                            {
                                if (jContent.TryGetValue(field, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                                {
                                    rowValues.Add(jToken.ToString());
                                }
                                else
                                {
                                    rowValues.Add(string.Empty);
                                }
                            }
                        }

                        result.Rows.Add(new KbQueryRow(rowValues));
                    }
                }
            }
            else //Indexed search!
            {
                foreach (var indexSelection in lookupOptimization.IndexSelection)
                {
                    Utility.EnsureNotNull(indexSelection.Index.DiskPath);

                    var indexPageCatalog = core.IO.GetPBuf<PersistIndexPageCatalog>(transaction, indexSelection.Index.DiskPath, LockOperation.Read);
                    Utility.EnsureNotNull(indexPageCatalog);

                    var foundDocumentIds = core.Indexes.MatchDocuments(indexPageCatalog, indexSelection, query);
                }

            }
            */

            /*
            try
            {
                using (var txRef = core.Transactions.Begin(transaction.ProcessId))
                {
                    PersistSchema schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbSchemaDoesNotExistException(schema);
                    }
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    var list = new List<PersistDocumentCatalogItem>();

                    var filePath = Path.Combine(schemaMeta.DiskPath, Constants.DocumentCatalogFile);
                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, filePath, LockOperation.Read);
                    Utility.EnsureNotNull(documentCatalog);

                    foreach (var item in documentCatalog.Collection)
                    {
                        list.Add(item);
                    }

                    txRef.Commit();

                    return list;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get catalog for process {processId}.", ex);
                throw;
            }
            */

            /*

            List<List<String>> rowValues = new List<List<string>>();
            int rowCount = 0;


            using (serverCore.ObjectLocks.Obtain(sessionId, LockType.Namespace, LockAccessType.Read, schemaPath))
            {
                IndexSelections indexSelections = serverCore.IndexOperations.SelectIndex(schemaPath, conditions, explanation);

                if (indexSelections != null && indexSelections.Count > 0)
                {
                    #region Index Scan, this is going to be quick!

                    List<int> intersectedDocuments = new List<int>();
                    bool firstLookup = true;

                    foreach (var indexSelection in indexSelections)
                    {
                        string indexPageCatalogFileName = Utility.MakePath(serverCore.Configuration.NamespacesPath, schemaPath, indexSelection.Index.Filename);
                        PersistIndexPageCatalog IndexPageCatalog = serverCore.IO.DeserializeFromProtoBufFile<PersistIndexPageCatalog>(indexPageCatalogFileName);

                        List<Condition> keyValues = new List<Condition>();

                        foreach (string attributeName in indexSelection.HandledKeyNames)
                        {
                            keyValues.Add((from o in conditions.Collection where o.Key == attributeName select o).First());
                        }

                        List<int> foundIndexPages = null;

                        //Get all index pages that match the key values.
                        if (indexSelection.Index.Attributes.Count == keyValues.Count)
                        {
                            if (indexSelection.Index.IndexType == IndexType.Unique)
                            {
                                planExplanationNode.Operation = PlanOperation.FullUniqueIndexMatchScan;
                            }
                            else
                            {
                                planExplanationNode.Operation = PlanOperation.FullIndexMatchScan;
                            }

                            foundIndexPages = IndexPageCatalog.FindDocuments(keyValues, planExplanationNode);
                        }
                        else
                        {
                            planExplanationNode.Operation = PlanOperation.PartialIndexMatchScan;
                            foundIndexPages = IndexPageCatalog.FindDocuments(keyValues, planExplanationNode);
                        }

                        //By default, FindPagesByPartialKey and FindPageByExactKey report ResultingNodes in "pages". Convert to documents.
                        planExplanationNode.ResultingNodes = foundIndexPages.Count;

                        if (firstLookup)
                        {
                            firstLookup = false;
                            //If we do not currently have any items in the result, then add the ones we just found.
                            intersectedDocuments.AddRange(foundIndexPages);
                        }
                        else
                        {
                            //Each time we do a subsequent lookup, find the intersection of the IDs from
                            //  this lookup and the previous looksup and make it our result.
                            //In this way, we continue to limit down the resulting rows by each subsequent index lookup.
                            intersectedDocuments = foundIndexPages.Intersect(intersectedDocuments).ToList();
                        }

                        planExplanationNode.IntersectedNodes = intersectedDocuments.Count;
                        planExplanationNode.Duration = explainStepDuration.Elapsed;
                        explanation.Steps.Add(planExplanationNode);

                        if (intersectedDocuments.Count == 0)
                        {
                            break; //Early termination, all rows eliminated.
                        }
                    }

                    List<Document> resultDocuments = new List<Document>();

                    var unindexedConditions = conditions.Collection.Where(p => indexSelections.UnhandledKeys.Any(p2 => p2 == p.Key)).ToList();

                    bool foundKey = false;
                    Stopwatch documentScanExplanationDuration = new Stopwatch();

                    PlanExplanationNode documentScanExplanationNode = null;

                    if (unindexedConditions.Count == 0 || intersectedDocuments.Count == 0)
                    {
                        foundKey = true;
                    }
                    else
                    {
                        documentScanExplanationDuration.Start();
                        documentScanExplanationNode = new PlanExplanationNode()
                        {
                            CoveredAttributes = (from o in unindexedConditions select o.Key).ToList(),
                            Operation = PlanOperation.DocumentScan
                        };
                    }

                    foreach (int documentId in intersectedDocuments)
                    {
                        if (documentScanExplanationNode != null)
                        {
                            documentScanExplanationNode.ScannedNodes++;
                        }

                        string persistDocumentFile = Utility.MakePath(serverCore.Configuration.NamespacesPath,
                            schemaPath,
                            PersistIndexPageCatalog.DocumentFileName(documentId));

                        timer.Restart();
                        PersistDocument persistDocument = serverCore.IO.DeserializeFromJsonFile<PersistDocument>(persistDocumentFile);
                        timer.Stop();
                        serverCore.PerformaceMetrics.AddDeserializeDocumentMs(timer.ElapsedMilliseconds);

                        bool fullAttributeMatch = true;

                        JObject jsonContent = null;

                        //If we have unindexed attributes, then open each of the documents from the previous index scans and compare the remining values.
                        if (unindexedConditions.Count > 0)
                        {
                            jsonContent = JObject.Parse(persistDocument.Text);

                            foreach (Condition condition in unindexedConditions)
                            {
                                JToken jToken = null;

                                if (jsonContent.TryGetValue(condition.Key, StringComparison.CurrentCultureIgnoreCase, out jToken))
                                {
                                    foundKey = true; //TODO: Implement this on the index scan!

                                    if (condition.IsMatch(jToken.ToString().ToLower()) == false)
                                    {
                                        fullAttributeMatch = false;
                                        break;
                                    }
                                }
                                else
                                {
                                    fullAttributeMatch = false;
                                    break;
                                }
                            }
                        }

                        if (fullAttributeMatch)
                        {
                            rowCount++;
                            if (rowLimit > 0 && rowCount > rowLimit)
                            {
                                break;
                            }

                            if (documentScanExplanationNode != null)
                            {
                                documentScanExplanationNode.ResultingNodes++;
                            }

                            if (hasFieldList)
                            {
                                if (jsonContent == null)
                                {
                                    jsonContent = JObject.Parse(persistDocument.Text);
                                }

                                List<string> fieldValues = new List<string>();

                                foreach (string fieldName in fieldList)
                                {
                                    if (fieldName == "#RID")
                                    {
                                        fieldValues.Add(persistDocument.Id.ToString());
                                    }
                                    else
                                    {
                                        JToken fieldToken = null;
                                        if (jsonContent.TryGetValue(fieldName, StringComparison.CurrentCultureIgnoreCase, out fieldToken))
                                        {
                                            fieldValues.Add(fieldToken.ToString());
                                        }
                                        else
                                        {
                                            fieldValues.Add(string.Empty);
                                        }
                                    }
                                }

                                rowValues.Add(fieldValues);
                            }
                            else
                            {
                                resultDocuments.Add(new Document
                                {
                                    Id = persistDocument.Id,
                                    OriginalType = persistDocument.OriginalType,
                                    Bytes = persistDocument.Bytes
                                });
                            }
                        }
                    }

                    if (documentScanExplanationNode != null)
                    {
                        documentScanExplanationNode.Duration = documentScanExplanationDuration.Elapsed;
                        documentScanExplanationNode.IntersectedNodes = resultDocuments.Count;
                        explanation.Steps.Add(documentScanExplanationNode);
                    }

                    #endregion

                    return new QueryResult
                    {
                        Message = foundKey ? string.Empty : "No attribute was found matching the given key(s).",
                        Success = true,
                        Documents = resultDocuments,
                        ExecutionTime = executionTime.Elapsed,
                        Explanation = explanation,
                        Columns = fieldList == null ? null : fieldList.ToList(),
                        Rows = rowValues,
                        RowCount = rowCount
                    };
                }
                else
                {
                    #region Document scan.... this is going to be tuff!

                    PlanExplanationNode planExplanationNode = new PlanExplanationNode(PlanOperation.FullDocumentScan)
                    {
                        CoveredAttributes = (from o in conditions.Collection select o.Key).ToList()
                    };

                    List<Document> resultDocuments = new List<Document>();

                    bool foundKey = false;

                    string documentCatalogFileName = Utility.MakePath(serverCore.Configuration.NamespacesPath, schemaPath, PersistDocumentCatalog.FileName);

                    timer.Restart();
                    PersistDocumentCatalog documentCatalog = serverCore.IO.DeserializeFromJsonFile<PersistDocumentCatalog>(documentCatalogFileName);
                    timer.Stop();
                    serverCore.PerformaceMetrics.AddDeserializeDocumentCatalogMs(timer.ElapsedMilliseconds);

                    foreach (PersistDocumentCatalogItem documentCatalogItem in documentCatalog.Collection)
                    {
                        string persistDocumentFile = Utility.MakePath(serverCore.Configuration.NamespacesPath, schemaPath, documentCatalogItem.DocumentFileName);

                        timer.Restart();
                        PersistDocument persistDocument = serverCore.IO.DeserializeFromJsonFile<PersistDocument>(persistDocumentFile);
                        timer.Stop();
                        serverCore.PerformaceMetrics.AddDeserializeDocumentMs(timer.ElapsedMilliseconds);

                        JObject jsonContent = JObject.Parse(persistDocument.Text);

                        bool fullAttributeMatch = true;

                        foreach (Condition condition in conditions.Collection)
                        {
                            JToken jToken = null;

                            if (jsonContent.TryGetValue(condition.Key, StringComparison.CurrentCultureIgnoreCase, out jToken))
                            {
                                foundKey = true; //TODO: Implement this on the index scan!

                                if (condition.IsMatch(jToken.ToString().ToLower()) == false)
                                {
                                    fullAttributeMatch = false;
                                    break;
                                }
                            }
                        }

                        if (fullAttributeMatch)
                        {
                            rowCount++;
                            if (rowLimit > 0 && rowCount > rowLimit)
                            {
                                break;
                            }

                            if (hasFieldList)
                            {
                                if (jsonContent == null)
                                {
                                    jsonContent = JObject.Parse(persistDocument.Text);
                                }

                                List<string> fieldValues = new List<string>();

                                foreach (string fieldName in fieldList)
                                {
                                    if (fieldName == "#RID")
                                    {
                                        fieldValues.Add(persistDocument.Id.ToString());
                                    }
                                    else
                                    {
                                        JToken fieldToken = null;
                                        if (jsonContent.TryGetValue(fieldName, StringComparison.CurrentCultureIgnoreCase, out fieldToken))
                                        {
                                            fieldValues.Add(fieldToken.ToString());
                                        }
                                        else
                                        {
                                            fieldValues.Add(string.Empty);
                                        }
                                    }
                                }

                                rowValues.Add(fieldValues);
                            }
                            else
                            {
                                resultDocuments.Add(new Document
                                {
                                    Id = persistDocument.Id,
                                    OriginalType = persistDocument.OriginalType,
                                    Bytes = persistDocument.Bytes
                                });
                            }

                            planExplanationNode.ResultingNodes++;
                        }

                        planExplanationNode.ScannedNodes++;

                        //------------------------------------------------------------------------------------------------------------------------------------------------
                    }

                    #endregion

                    planExplanationNode.IntersectedNodes = planExplanationNode.ScannedNodes;

                    explanation.Steps.Add(planExplanationNode);

                    return new QueryResult
                    {
                        Message = foundKey ? string.Empty : "No attribute was found matching the given key(s).",
                        Success = true,
                        Documents = resultDocuments,
                        ExecutionTime = executionTime.Elapsed,
                        Explanation = explanation,
                        Columns = fieldList == null ? null : fieldList.ToList(),
                        Rows = rowValues,
                        RowCount = rowCount
                    };
                }
            } //End lock.
            */
        }

        /// <summary>
        /// Saves a new document, this is used for inserts.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        /// <param name="newId"></param>
        /// <exception cref="KbSchemaDoesNotExistException"></exception>
        public void Store(ulong processId, string schema, KbDocument document, out Guid? newId)
        {
            try
            {
                var persistDocument = PersistDocument.FromPayload(document);

                if (persistDocument.Id == null || persistDocument.Id == Guid.Empty)
                {
                    persistDocument.Id = Guid.NewGuid();
                }
                if (persistDocument.Created == null || persistDocument.Created == DateTime.MinValue)
                {
                    persistDocument.Created = DateTime.UtcNow;
                }
                if (persistDocument.Modfied == null || persistDocument.Modfied == DateTime.MinValue)
                {
                    persistDocument.Modfied = DateTime.UtcNow;
                }

                using (var txRef = core.Transactions.Begin(processId))
                {
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Write);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbSchemaDoesNotExistException(schema);
                    }
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    string documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, Constants.DocumentCatalogFile);

                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);
                    Utility.EnsureNotNull(documentCatalog);

                    documentCatalog.Add(persistDocument);
                    core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);

                    string documentDiskPath = Path.Combine(schemaMeta.DiskPath, Helpers.GetDocumentModFilePath((Guid)persistDocument.Id));
                    core.IO.CreateDirectory(txRef.Transaction, Path.GetDirectoryName(documentDiskPath));
                    core.IO.PutJson(txRef.Transaction, documentDiskPath, persistDocument);

                    core.Indexes.InsertDocumentIntoIndexes(txRef.Transaction, schemaMeta, persistDocument);

                    newId = persistDocument.Id;

                    txRef.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to store document for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes a document by its ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <param name="newId"></param>
        /// <exception cref="KbSchemaDoesNotExistException"></exception>
        public void DeleteById(ulong processId, string schema, Guid newId)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Write);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbSchemaDoesNotExistException(schema);
                    }

                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    string documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, Constants.DocumentCatalogFile);

                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);

                    Utility.EnsureNotNull(documentCatalog);

                    var persistDocument = documentCatalog.GetById(newId);
                    if (persistDocument != null)
                    {
                        string documentDiskPath = Path.Combine(schemaMeta.DiskPath, Helpers.GetDocumentModFilePath(persistDocument.Id));

                        core.IO.DeleteFile(txRef.Transaction, documentDiskPath);

                        documentCatalog.Remove(persistDocument);

                        core.Indexes.DeleteDocumentFromIndexes(txRef.Transaction, schemaMeta, persistDocument.Id);

                        core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);
                    }

                    txRef.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete document by ID for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns a list of all documents in a schema.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        /// <exception cref="KbSchemaDoesNotExistException"></exception>
        public List<PersistDocumentCatalogItem> EnumerateCatalog(ulong processId, string schema)
        {
            try
            {
                using (var transaction = core.Transactions.Begin(processId))
                {
                    PersistSchema schemaMeta = core.Schemas.VirtualPathToMeta(transaction.Transaction, schema, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbSchemaDoesNotExistException(schema);
                    }
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    var filePath = Path.Combine(schemaMeta.DiskPath, Constants.DocumentCatalogFile);
                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(transaction.Transaction, filePath, LockOperation.Read);
                    Utility.EnsureNotNull(documentCatalog);

                    var list = new List<PersistDocumentCatalogItem>();
                    foreach (var item in documentCatalog.Collection)
                    {
                        list.Add(item);
                    }
                    transaction.Commit();

                    return list;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get catalog for process {processId}.", ex);
                throw;
            }
        }
    }
}
