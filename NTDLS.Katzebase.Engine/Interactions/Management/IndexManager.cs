using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.QueryProcessing.Functions;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Parsers.Indexes.Matching;
using NTDLS.Katzebase.Parsers.Query;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.PersistentTypes.Index;
using NTDLS.Katzebase.Shared;
using System.Text;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to indexes.
    /// </summary>
    public class IndexManager
    {
        private readonly EngineCore _core;
        internal IndexQueryHandlers QueryHandlers { get; private set; }
        public IndexAPIHandlers APIHandlers { get; private set; }

        internal IndexManager(EngineCore core)
        {
            _core = core;
            try
            {
                QueryHandlers = new IndexQueryHandlers(core);
                APIHandlers = new IndexAPIHandlers(core);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate index manager.", ex);
                throw;
            }
        }

        #region Create / Analyze / Rebuild / Drop.

        internal void CreateIndex(Transaction transaction, string schemaName, KbIndex index, out Guid newId)
        {
            try
            {
                var physicalIndex = PhysicalIndex.FromClientPayload(index);

                physicalIndex.Id = Guid.NewGuid();
                physicalIndex.Created = DateTime.UtcNow;
                physicalIndex.Modified = DateTime.UtcNow;

                if (physicalIndex.Partitions <= 0)
                {
                    physicalIndex.Partitions = _core.Settings.DefaultIndexPartitions;
                }

                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Write);

                if (indexCatalog.GetByName(index.Name) != null)
                {
                    throw new KbObjectAlreadyExistsException($"Index already exists: [{index.Name}].");
                }

                indexCatalog.Add(physicalIndex);

                if (indexCatalog.DiskPath == null || physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null: [{nameof(physicalSchema.DiskPath)}].");
                }

                _core.IO.PutJson(transaction, indexCatalog.DiskPath, indexCatalog);

                RebuildIndex(transaction, physicalSchema, physicalIndex);

                newId = physicalIndex.Id;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to create index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal string AnalyzeIndex(Transaction transaction, string schemaName, string indexName)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);
                if (indexCatalog.DiskPath == null || physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null: [{nameof(physicalSchema.DiskPath)}].");
                }

                var physicalIndex = indexCatalog.GetByName(indexName) ?? throw new KbObjectNotFoundException($"Index not found: [{indexName}].");

                var physicalIndexPageMap = new Dictionary<uint, PhysicalIndexPages>();
                var physicalIndexPageMapDistilledLeaves = new List<List<PhysicalIndexLeaf>>();

                double diskSize = 0;
                double decompressedSiskSize = 0;

                int rootNodes = 0;

                for (uint indexPartition = 0; indexPartition < physicalIndex.Partitions; indexPartition++)
                {
                    string pageDiskPath = physicalIndex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                    physicalIndexPageMap[indexPartition] = _core.IO.GetPBuf<PhysicalIndexPages>(transaction, pageDiskPath, LockOperation.Read);
                    diskSize += _core.IO.GetDecompressedSizeTracked(pageDiskPath);
                    decompressedSiskSize += new FileInfo(pageDiskPath).Length;
                    physicalIndexPageMapDistilledLeaves.Add(DistillIndexBaseNodes(physicalIndexPageMap[indexPartition].Root));
                    rootNodes += physicalIndexPageMap[indexPartition].Root.Children.Count;

                }

                var combinedNodes = physicalIndexPageMapDistilledLeaves.SelectMany(o => o);

                int minDocumentsPerNode = combinedNodes.Min(o => o.Documents?.Count ?? 0);
                int maxDocumentsPerNode = combinedNodes.Max(o => o.Documents?.Count) ?? 0;
                double avgDocumentsPerNode = combinedNodes.Average(o => o.Documents?.Count) ?? 0;
                int documentCount = combinedNodes.Sum(o => o.Documents?.Count ?? 0);
                double selectivityScore = 100.0;

                if (documentCount > 0)
                {
                    selectivityScore = 100.0 - avgDocumentsPerNode / documentCount * 100.0;
                }

                var builder = new StringBuilder();
                builder.AppendLine("Index Analysis {");
                builder.AppendLine($"    Schema            : {physicalSchema.Name}");
                builder.AppendLine($"    Name              : {physicalIndex.Name}");
                builder.AppendLine($"    Partitions        : {physicalIndex.Partitions}");
                builder.AppendLine($"    Id                : {physicalIndex.Id}");
                builder.AppendLine($"    Unique            : {physicalIndex.IsUnique}");
                builder.AppendLine($"    Created           : {physicalIndex.Created}");
                builder.AppendLine($"    Modified          : {physicalIndex.Modified}");
                builder.AppendLine($"    Disk Path         : {physicalIndex.GetPartitionPagesPath(physicalSchema)}");
                builder.AppendLine($"    Pages Size        : {diskSize / 1024.0:N2}k");
                builder.AppendLine($"    Disk Size         : {decompressedSiskSize / 1024.0:N2}k");
                builder.AppendLine($"    Compression Ratio : {decompressedSiskSize / diskSize * 100.0:N2}");
                builder.AppendLine($"    Node Count        : {combinedNodes.Sum(o => o.Documents?.Count ?? 0):N0}");
                builder.AppendLine($"    Root Node Count   : {rootNodes:N0}");
                builder.AppendLine($"    Distinct Nodes    : {combinedNodes.Count():N0}");
                builder.AppendLine($"    Max. Node Depth   : {physicalIndex.Attributes.Count:N0}");
                builder.AppendLine($"    Min. Node Density : {minDocumentsPerNode:N0}");
                builder.AppendLine($"    Max. Node Density : {maxDocumentsPerNode:N0}" + (maxDocumentsPerNode == 1 ? " (unique)" : ""));
                builder.AppendLine($"    Avg. Node Density : {avgDocumentsPerNode:N2}");
                builder.AppendLine($"    Document Count    : {documentCount:N0}");
                builder.AppendLine($"    Selectivity Score : {selectivityScore:N4}%");

                builder.AppendLine("    Attributes {");
                foreach (var attrib in physicalIndex.Attributes)
                {
                    builder.AppendLine($"        {attrib.Field}");
                }
                builder.AppendLine("    }");
                builder.AppendLine("}");

                return builder.ToString();
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to analyze index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void DropIndex(Transaction transaction, string schemaName, string indexName)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Write);
                if (indexCatalog.DiskPath == null || physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null: [{nameof(physicalSchema.DiskPath)}].");
                }

                var physicalIndex = indexCatalog.GetByName(indexName);
                if (physicalIndex != null)
                {
                    indexCatalog.Remove(physicalIndex);

                    if (Path.Exists(physicalIndex.GetPartitionPagesPath(physicalSchema)))
                    {
                        _core.IO.DeletePath(transaction, physicalIndex.GetPartitionPagesPath(physicalSchema));
                    }

                    _core.IO.PutJson(transaction, indexCatalog.DiskPath, indexCatalog);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to drop index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        #region Match Schema Documents by Conditions.

        /// <summary>
        /// Used for indexing operations for a groups of conditions.
        /// </summary>
        /// <param name="keyValues">For JOIN operations, contains the values of the joining document.
        /// For WHERE clause, values are stored in the conditions so this is not needed.</param>
        /// <returns></returns>
        internal Dictionary<uint, DocumentPointer> MatchSchemaDocumentsByConditionsClause(
                    PhysicalSchema physicalSchema, IndexingConditionOptimization optimization,
                    PreparedQuery query, string workingSchemaPrefix, KbInsensitiveDictionary<string?>? keyValues = null)
        {
            Dictionary<uint, DocumentPointer>? accumulatedResults = null;

            var ptIndexSearch = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.IndexSearch, $"Schema: {workingSchemaPrefix}");

            //We aggregate the values for the entries into the ConditionGroup.IndexLookup,
            //  which contains all of the values for all entries in the group.
            //  For this reason, we do not perform index lookups on individual condition entries.
            foreach (var group in optimization.Conditions.Collection.OfType<ConditionGroup>().Where(group => group.IndexLookup != null))
            {
                var groupResults = MatchSchemaDocumentsByConditionsClauseRecursive(
                    physicalSchema, optimization, group, query, workingSchemaPrefix, keyValues);

                if (group.Connector == LogicalConnector.Or)
                {
                    accumulatedResults ??= new(); //Really though, we should never start with an OR connector...

                    var ptDocumentPointerUnion = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerUnion);
                    accumulatedResults.UnionWith(groupResults);
                    ptDocumentPointerUnion?.StopAndAccumulate();
                }
                else // LogicalConnector.And || LogicalConnector.None
                {
                    var ptDocumentPointerIntersect = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                    accumulatedResults = accumulatedResults.IntersectWith(groupResults);
                    ptDocumentPointerIntersect?.StopAndAccumulate();
                }
            }

            ptIndexSearch?.StopAndAccumulate();

            return accumulatedResults ?? new();
        }

        private Dictionary<uint, DocumentPointer> MatchSchemaDocumentsByConditionsClauseRecursive(
            PhysicalSchema physicalSchema, IndexingConditionOptimization optimization, ConditionGroup givenConditionGroup,
            PreparedQuery query, string workingSchemaPrefix, KbInsensitiveDictionary<string?>? keyValues = null)
        {
            var thisGroupResults = MatchSchemaDocumentsByIndexingConditionLookup(optimization.Transaction,
                query, givenConditionGroup.IndexLookup.EnsureNotNull(), physicalSchema, workingSchemaPrefix, keyValues);

            foreach (var group in givenConditionGroup.Collection.OfType<ConditionGroup>().Where(o => o.IndexLookup != null))
            {
                var childGroupResults = MatchSchemaDocumentsByConditionsClauseRecursive(
                     physicalSchema, optimization, group, query, workingSchemaPrefix, keyValues);

                if (group.Connector == LogicalConnector.Or)
                {
                    var ptDocumentPointerUnion = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerUnion);
                    thisGroupResults.UnionWith(childGroupResults);
                    ptDocumentPointerUnion?.StopAndAccumulate();
                }
                else // LogicalConnector.And || LogicalConnector.None
                {
                    var ptDocumentPointerIntersect = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                    thisGroupResults = thisGroupResults.IntersectWith(childGroupResults);
                    ptDocumentPointerIntersect?.StopAndAccumulate();
                }
            }

            return thisGroupResults;
        }

        private Dictionary<uint, DocumentPointer> MatchSchemaDocumentsByIndexingConditionLookup(Transaction transaction, PreparedQuery query,
            IndexingConditionLookup indexLookup, PhysicalSchema physicalSchema, string workingSchemaPrefix, KbInsensitiveDictionary<string?>? keyValues)
        {
            Dictionary<uint, DocumentPointer>? accumulatedResults = null;

            try
            {
                var conditionEntries = indexLookup.AttributeConditionSets[indexLookup.IndexSelection.PhysicalIndex.Attributes[0].Field.EnsureNotNull()];

                foreach (var condition in conditionEntries)
                {
                    var indexPartitions = new List<uint>();

                    if (condition.Qualifier == LogicalQualifier.Equals)
                    {
                        //For join operations, check the keyValues for the raw value to lookup.
                        if (keyValues?.TryGetValue(condition.Right.Value, out string? keyValue) != true)
                        {
                            if (condition.Right is QueryFieldCollapsedValue collapsedValue)
                            {
                                keyValue = collapsedValue.Value;
                            }
                            else
                            {
                                keyValue = condition.Right.CollapseScalarQueryField(transaction, query, query.SelectFields, keyValues ?? new())?.ToLowerInvariant();
                            }
                        }
                        else
                        {
                            //This is most likely a join clause... or we have something wrong.
                        }

                        //Eliminated all but one index partitions.
                        indexPartitions.Add(indexLookup.IndexSelection.PhysicalIndex.ComputePartition(keyValue));
                    }
                    else
                    {
                        //We have to search all index partitions.
                        for (uint indexPartition = 0; indexPartition < indexLookup.IndexSelection.PhysicalIndex.Partitions; indexPartition++)
                        {
                            indexPartitions.Add(indexPartition);
                        }
                    }

                    var allThreadResultsForSingleCondition = new Dictionary<uint, DocumentPointer>();

                    var childPool = _core.ThreadPool.Indexing.CreateChildPool<uint>(_core.Settings.IndexingChildThreadPoolQueueDepth);

                    foreach (var indexPartition in indexPartitions)
                    {
                        var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                        childPool.Enqueue(indexPartition, (threadIndexPartition) =>
                        {
                            #region Thread.

                            try
                            {
                                Dictionary<uint, DocumentPointer> singleThreadResults = new();
                                string pageDiskPath = indexLookup.IndexSelection.PhysicalIndex.GetPartitionPagesFileName(physicalSchema, threadIndexPartition);
                                var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(transaction, pageDiskPath, LockOperation.Read);

                                List<PhysicalIndexLeaf> workingPhysicalIndexLeaves = [physicalIndexPages.Root];
                                if (workingPhysicalIndexLeaves.Count > 0)
                                {
                                    //First process the condition at the AttributeDepth that was passed in.
                                    workingPhysicalIndexLeaves = MatchIndexLeaves(transaction, query,
                                    condition, workingPhysicalIndexLeaves, query.Conditions.FieldCollection, keyValues);

                                    if (indexLookup.IndexSelection.PhysicalIndex.Attributes.Count == 1)
                                    {
                                        //The index only has one attribute, so we are at the base where the document pointers are.
                                        //(workingPhysicalIndexLeaves.FirstOrDefault()?.Documents?.Count > 0) //We found documents, we are at the base of the index.
                                        var ptIndexDistillation = transaction.Instrumentation.CreateToken(PerformanceCounter.IndexDistillation);
                                        singleThreadResults = DistillIndexLeaves(workingPhysicalIndexLeaves);
                                        ptIndexDistillation?.StopAndAccumulate();

                                    }
                                    else if (workingPhysicalIndexLeaves.Count > 0)
                                    {
                                        //Further, recursively, process additional compound index attribute condition matches.
                                        singleThreadResults = MatchSchemaDocumentsByIndexingConditionLookupRecursive(
                                            transaction, query, indexLookup, keyValues, 1, workingPhysicalIndexLeaves);
                                    }
                                }

                                if (singleThreadResults.Count > 0)
                                {
                                    var ptDocumentPointerUnion = transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerUnion);
                                    lock (allThreadResultsForSingleCondition)
                                    {
                                        allThreadResultsForSingleCondition.UnionWith(singleThreadResults);
                                    }
                                    ptDocumentPointerUnion?.StopAndAccumulate();
                                }
                            }
                            catch (Exception ex)
                            {
                                LogManager.Error($"Failed to match index by thread.", ex);
                                throw;
                            }

                            #endregion
                        });
                        ptThreadQueue?.StopAndAccumulate();
                    }

                    var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion, $"Index: {indexLookup.IndexSelection.PhysicalIndex.Name}");
                    childPool.WaitForCompletion();
                    ptThreadCompletion?.StopAndAccumulate();

                    var ptDocumentPointerIntersect = transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                    accumulatedResults = accumulatedResults.IntersectWith(allThreadResultsForSingleCondition);
                    ptDocumentPointerIntersect?.StopAndAccumulate();

                    //LogManager.Debug($"Depth: root, Count: {operation.ThreadResults.Count}, Total: {accumulatedResults.Count}");
                    if (accumulatedResults.Count == 0)
                    {
                        break; //Condition eliminated all possible results on this level.
                    }
                }

                return accumulatedResults ?? new();
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to match index documents for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private static Dictionary<uint, DocumentPointer> MatchSchemaDocumentsByIndexingConditionLookupRecursive(
            Transaction transaction,
            PreparedQuery query,
            IndexingConditionLookup Lookup,
            KbInsensitiveDictionary<string?>? keyValues,
             int attributeDepth, List<PhysicalIndexLeaf> workingPhysicalIndexLeaves)
        {
            Dictionary<uint, DocumentPointer>? results = null;

            var conditionSet = Lookup.AttributeConditionSets[
                Lookup.IndexSelection.PhysicalIndex.Attributes[attributeDepth].Field.EnsureNotNull()];

            foreach (var condition in conditionSet)
            {
                var partitionResults = MatchIndexLeaves(transaction, query,
                    condition, workingPhysicalIndexLeaves, query.Conditions.FieldCollection, keyValues);

                if (attributeDepth == Lookup.AttributeConditionSets.Count - 1)
                {
                    //This is the bottom of the condition tree, as low as we can go in this index given the fields we have, so just distill the leaves.

                    var ptIndexDistillation = transaction.Instrumentation.CreateToken(PerformanceCounter.IndexDistillation);
                    var partialResults = DistillIndexLeaves(partitionResults);
                    ptIndexDistillation?.StopAndAccumulate();

                    var ptDocumentPointerIntersect = transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                    results = results.IntersectWith(partialResults);
                    ptDocumentPointerIntersect?.StopAndAccumulate();

                    //LogManager.Debug($"Partition: {parameter.IndexPartition}, Depth: {attributeDepth}, Count: {partialResults.Count}, Total: {results.Count}");
                    if (results.Count == 0)
                    {
                        break; //Condition eliminated all possible results on this level.
                    }
                }
                else if (attributeDepth < Lookup.AttributeConditionSets.Count - 1)
                {
                    //Further, recursively, process additional compound index attribute condition matches.
                    var partialResults = MatchSchemaDocumentsByIndexingConditionLookupRecursive(
                        transaction, query, Lookup, keyValues, attributeDepth + 1, partitionResults);

                    var ptDocumentPointerIntersect = transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                    results = results.IntersectWith(partialResults);
                    ptDocumentPointerIntersect?.StopAndAccumulate();

                    //LogManager.Debug($"Partition: {parameter.IndexPartition}, Depth: {attributeDepth}, Count: {partialResults.Count}, Total: {results.Count}");
                    if (results.Count == 0)
                    {
                        break; //Condition eliminated all possible results on this level.
                    }
                }
            }

            return results ?? new();
        }

        #endregion

        #region Matching / Seeking / Scanning.

        private static List<PhysicalIndexLeaf> MatchIndexLeaves(Transaction transaction, PreparedQuery query, ConditionEntry condition,
            List<PhysicalIndexLeaf> workingPhysicalIndexLeaves, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<string?>? auxiliaryFields)
        {
            //For join operations, check the keyValues for the raw value to lookup.
            if (auxiliaryFields?.TryGetValue(condition.Right.Value, out string? keyValue) != true)
            {
                if (condition.Right is QueryFieldCollapsedValue collapsedValue)
                {
                    keyValue = collapsedValue.Value;
                }
                else
                {
                    keyValue = condition.Right.CollapseScalarQueryField(transaction, query, fieldCollection, auxiliaryFields ?? new())?.ToLowerInvariant();
                }
            }

            return condition.Qualifier switch
            {
                LogicalQualifier.Equals => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry.IsMatchEqual(transaction, w.Key, keyValue) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.NotEquals => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry.IsMatchEqual(transaction, w.Key, keyValue) == false)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.GreaterThan => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry.IsMatchGreater(transaction, w.Key, keyValue) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.GreaterThanOrEqual => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry.IsMatchGreaterOrEqual(transaction, w.Key, keyValue) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.LessThan => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry.IsMatchLesser(transaction, w.Key, keyValue) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.LessThanOrEqual => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry.IsMatchLesserOrEqual(transaction, w.Key, keyValue) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.Like => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry.IsMatchLike(transaction, w.Key, keyValue) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.NotLike => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry.IsMatchLike(transaction, w.Key, keyValue) == false)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.Between => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry.IsMatchBetween(transaction, w.Key, keyValue) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.NotBetween => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry.IsMatchBetween(transaction, w.Key, keyValue) == false)
                                        .Select(s => s.Value)).ToList(),
                _ => throw new KbNotImplementedException($"Logical qualifier has not been implemented for indexing: [{condition.Qualifier}]"),
            };
        }

        #endregion

        #region Distillation.

        /// <summary>
        /// Traverse to the bottom of the index tree (from whatever starting point is passed in) and return
        /// a list of all nodes containing documents.
        /// </summary>
        /// <param name="indexEntires"></param>
        /// <returns></returns>
        private static List<PhysicalIndexLeaf> DistillIndexBaseNodes(PhysicalIndexLeaf physicalIndexLeaf)
        {
            try
            {
                var result = new List<PhysicalIndexLeaf>();

                void DistillIndexBaseNodesRecursive(PhysicalIndexLeaf physicalIndexLeaf)
                {
                    foreach (var child in physicalIndexLeaf.Children)
                    {
                        DistillIndexBaseNodesRecursive(child.Value);
                    }

                    if (physicalIndexLeaf?.Documents?.Count > 0)
                    {
                        result.Add(physicalIndexLeaf);
                    }
                }

                if (physicalIndexLeaf?.Documents?.Count > 0)
                {
                    result.Add(physicalIndexLeaf);
                }
                else if (physicalIndexLeaf?.Children != null)
                {
                    foreach (var child in physicalIndexLeaf.Children)
                    {
                        DistillIndexBaseNodesRecursive(child.Value);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to distill index base nodes.", ex);
                throw;
            }
        }

        private static Dictionary<uint, DocumentPointer> DistillIndexLeaves(List<PhysicalIndexLeaf> physicalIndexLeaves)
        {
            var result = new List<DocumentPointer>();

            foreach (var leaf in physicalIndexLeaves)
            {
                result.AddRange(DistillIndexLeaves(leaf));
            }

            return result.ToDictionary(o => o.DocumentId, o => o);
        }

        /// <summary>
        /// Traverse to the bottom of the index tree (from whatever starting point is passed in) and return a list of all documentIds.
        /// </summary>
        /// <param name="indexEntires"></param>
        /// <returns></returns>
        private static List<DocumentPointer> DistillIndexLeaves(PhysicalIndexLeaf physicalIndexLeaf)
        {
            try
            {
                var result = new List<DocumentPointer>();

                void DistillIndexLeavesRecursive(PhysicalIndexLeaf physicalIndexLeaf)
                {
                    foreach (var child in physicalIndexLeaf.Children)
                    {
                        DistillIndexLeavesRecursive(child.Value);
                    }

                    if (physicalIndexLeaf?.Documents?.Count > 0)
                    {
                        result.AddRange(physicalIndexLeaf.Documents.Select(o => new DocumentPointer(o.PageNumber, o.DocumentId)));
                    }
                }

                if (physicalIndexLeaf?.Documents?.Count > 0)
                {
                    result.AddRange(physicalIndexLeaf.Documents.Select(o => new DocumentPointer(o.PageNumber, o.DocumentId)));
                }
                else if (physicalIndexLeaf?.Children != null)
                {
                    foreach (var child in physicalIndexLeaf.Children)
                    {
                        DistillIndexLeavesRecursive(child.Value);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to distill index leaves.", ex);
                throw;
            }
        }

        #endregion

        #region Index Insert.

        /// <summary>
        /// Inserts an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocumentIntoIndexes(Transaction transaction,
            PhysicalSchema physicalSchema, PhysicalDocument physicalDocument, DocumentPointer documentPointer)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIndex in indexCatalog.Collection)
                {
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIndex, physicalDocument, documentPointer);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to insert document into indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocumentsIntoIndex(Transaction transaction,
            PhysicalSchema physicalSchema, PhysicalIndex physicalIndex, Dictionary<DocumentPointer, PhysicalDocument> documents)
        {
            try
            {
                foreach (var document in documents)
                {
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIndex, document.Value, document.Key);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to insert document into indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }


        /// <summary>
        /// Inserts an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocumentsIntoIndexes(Transaction transaction,
            PhysicalSchema physicalSchema, Dictionary<DocumentPointer, PhysicalDocument> documents)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                foreach (var document in documents)
                {
                    //Loop though each index in the schema.
                    foreach (var physicalIndex in indexCatalog.Collection)
                    {
                        InsertDocumentIntoIndex(transaction, physicalSchema, physicalIndex, document.Value, document.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to insert document into indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index using the file name from the index object.
        /// </summary>
        private void InsertDocumentIntoIndex(Transaction transaction,
            PhysicalSchema physicalSchema, PhysicalIndex physicalIndex, PhysicalDocument document, DocumentPointer documentPointer)
        {
            try
            {
                var documentField = physicalIndex.Attributes[0].Field;
                document.Elements.TryGetValue(documentField.EnsureNotNull(), out string? value);

                uint indexPartition = physicalIndex.ComputePartition(value);

                string pageDiskPath = physicalIndex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(transaction, pageDiskPath, LockOperation.Write);

                InsertDocumentIntoIndexPages(transaction, physicalIndex, physicalIndexPages, document, documentPointer);

                _core.IO.PutPBuf(transaction, pageDiskPath, physicalIndexPages);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to insert document into index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index using a long lived index page catalog.
        /// </summary>
        private static void InsertDocumentIntoIndexPages(Transaction transaction, PhysicalIndex physicalIndex, PhysicalIndexPages physicalIndexPages, PhysicalDocument document, DocumentPointer documentPointer)
        {
            try
            {
                var searchTokens = GetIndexSearchTokens(transaction, physicalIndex, document);

                var indexScanResult = LocateExtentInGivenIndexPageCatalog(transaction, searchTokens, physicalIndexPages);

                //If we found a full match for all supplied key values - add the document to the leaf collection.
                if (indexScanResult.MatchType == IndexMatchType.Full)
                {
                    indexScanResult.Leaf.EnsureNotNull().Documents ??= new();

                    if (physicalIndex.IsUnique && indexScanResult.Leaf.Documents.EnsureNotNull().Count > 1)
                    {
                        throw new KbDuplicateKeyViolationException($"Duplicate key violation occurred for index [{physicalIndex.Name}], values: [{string.Join("],[", searchTokens)}]");
                    }
                }
                else
                {
                    //If we didn't find a full match for all supplied key values, then create the tree and add the document to the
                    //  lowest leaf. Note that we are going to start creating the leaf level at the findResult.ExtentLevel. This is
                    //  because we may have a partial match and don't need to create the full tree.

                    for (int i = indexScanResult.ExtentLevel; i < searchTokens.Count; i++)
                    {
                        indexScanResult.Leaf = indexScanResult.Leaf.EnsureNotNull().AddNewLeaf(searchTokens[i]);
                    }


                    indexScanResult.Leaf.EnsureNotNull().Documents ??= new();
                }

                //Add the document to the lowest index extent.
                indexScanResult.Leaf.Documents.EnsureNotNull().Add(
                    new PhysicalIndexEntry(documentPointer.DocumentId, documentPointer.PageNumber));
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to insert document into index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private static List<string> GetIndexSearchTokens(Transaction transaction, PhysicalIndex physicalIndex, PhysicalDocument document)
        {
            try
            {
                var result = new List<string>();

                foreach (var indexAttribute in physicalIndex.Attributes)
                {
                    if (document.Elements.TryGetValue(indexAttribute.Field.EnsureNotNull(), out string? documentValue))
                    {
                        if (documentValue != null) //TODO: How do we handle indexed NULL values?
                        {
                            result.Add(documentValue.ToLowerInvariant());
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to get index search tokens for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Finds the appropriate index page for a set of key values in the given index page catalog.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalIndex"></param>
        /// <param name="searchTokens"></param>
        /// <param name="indexPageCatalog"></param>
        /// <returns>A reference to a node in the suppliedIndexPageCatalog</returns>
        private static IndexScanResult LocateExtentInGivenIndexPageCatalog(
            Transaction transaction, List<string> searchTokens, PhysicalIndexPages rootPhysicalIndexPages)
        {
            try
            {
                var physicalIndexPages = rootPhysicalIndexPages;

                var result = new IndexScanResult()
                {
                    Leaf = physicalIndexPages.Root,
                    MatchType = IndexMatchType.None
                };

                if (physicalIndexPages.Root.Children == null || physicalIndexPages.Root.Children.Count == 0)
                {
                    return result; //The index is empty.
                }

                foreach (var token in searchTokens)
                {
                    if (result.Leaf.Children == null)
                    {
                        break;
                    }

                    if (result.Leaf.Children.TryGetValue(token, out PhysicalIndexLeaf? value))
                    {
                        result.ExtentLevel++;
                        result.Leaf = value; //Move one level lower in the extent tree.
                    }
                    else
                    {
                        break;
                    }
                }

                if (result.ExtentLevel > 0)
                {
                    result.MatchType = result.ExtentLevel == searchTokens.Count ? IndexMatchType.Full : IndexMatchType.Partial;
                }

                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to locate index extent for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        #region Index Update.

        /// <summary>
        /// Updates an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void UpdateDocumentsIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema,
            Dictionary<DocumentPointer, PhysicalDocument> documents, IEnumerable<string>? listOfModifiedFields)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                foreach (var physicalIndex in indexCatalog.Collection)
                {
                    if (listOfModifiedFields == null || physicalIndex.Attributes.Any(o => listOfModifiedFields.Contains(o.Field)))
                    {
                        RemoveDocumentsFromIndex(transaction, physicalSchema, physicalIndex, documents.Select(o => o.Key));
                        InsertDocumentsIntoIndex(transaction, physicalSchema, physicalIndex, documents);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to update document into indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        #region Index Delete.

        /// <summary>
        /// Removes a collection of document from all of the indexes on the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentPointer"></param>
        internal void RemoveDocumentsFromIndexes(Transaction transaction,
            PhysicalSchema physicalSchema, IEnumerable<DocumentPointer> documentPointers)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIndex in indexCatalog.Collection)
                {
                    RemoveDocumentsFromIndex(transaction, physicalSchema, physicalIndex, documentPointers);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to delete document from indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private long RemoveDocumentsFromLeaves(PhysicalIndexLeaf leaf, IEnumerable<DocumentPointer> documentPointers)
        {
            return RemoveDocumentsFromLeaves(leaf, documentPointers, documentPointers.Count());
        }

        private long RemoveDocumentsFromLeaves(PhysicalIndexLeaf leaf, IEnumerable<DocumentPointer> documentPointers, long maxCount)
        {
            long totalDeletes = 0;

            try
            {
                if (leaf.Documents?.Count > 0)
                {
                    foreach (var documentPointer in documentPointers)
                    {
                        totalDeletes += leaf?.Documents.RemoveAll(o => o.PageNumber == documentPointer.PageNumber
                                                                    && o.DocumentId == documentPointer.DocumentId) ?? 0;
                        if (totalDeletes == maxCount)
                        {
                            break;
                        }

                    }
                    return totalDeletes;
                }

                foreach (var child in leaf.Children)
                {
                    totalDeletes += RemoveDocumentsFromLeaves(child.Value, documentPointers); //Dig to the bottom of each branch using recursion.
                    if (totalDeletes == maxCount)
                    {
                        break;
                    }
                }

                return totalDeletes;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to remove documents from index leaves.", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes a collection of documents from an index. Locks the index page catalog for write.
        /// </summary>
        private void RemoveDocumentsFromIndex(Transaction transaction, PhysicalSchema physicalSchema,
            PhysicalIndex physicalIndex, IEnumerable<DocumentPointer> documentPointers)
        {
            try
            {
                var childPool = _core.ThreadPool.Indexing.CreateChildPool<uint>(_core.Settings.IndexingChildThreadPoolQueueDepth);

                for (uint indexPartition = 0; indexPartition < physicalIndex.Partitions; indexPartition++)
                {
                    if (childPool.ExceptionOccurred())
                    {
                        break;
                    }

                    var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                    childPool.Enqueue(indexPartition, ( threadIndexPartition) =>
                    {
                        #region Thread.

                        transaction.EnsureActive();

                        string pageDiskPath = physicalIndex.GetPartitionPagesFileName(
                            physicalSchema, threadIndexPartition);

                        var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(transaction, pageDiskPath, LockOperation.Write);

                        if (RemoveDocumentsFromLeaves(physicalIndexPages.Root, documentPointers) > 0)
                        {
                            _core.IO.PutPBuf(transaction, pageDiskPath, physicalIndexPages);
                        }

                        #endregion
                    });
                    ptThreadQueue?.StopAndAccumulate();
                }

                var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion, $"Index: {physicalIndex.Name}");
                childPool.WaitForCompletion();
                ptThreadCompletion?.StopAndAccumulate();

            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to remove documents from index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }


        #endregion

        #region Rebuild Index.

        internal void RebuildIndex(Transaction transaction, string schemaName, string indexName, uint newPartitionCount = 0)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Write);
                if (indexCatalog.DiskPath == null || physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null: [{nameof(physicalSchema.DiskPath)}].");
                }

                var physicalIndex = indexCatalog.GetByName(indexName) ?? throw new KbObjectNotFoundException($"Index not found: [{indexName}].");

                if (newPartitionCount != 0)
                {
                    physicalIndex.Partitions = newPartitionCount;
                }

                RebuildIndex(transaction, physicalSchema, physicalIndex);

                physicalIndex.Modified = DateTime.UtcNow;

                _core.IO.PutJson(transaction, indexCatalog.DiskPath, indexCatalog);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to rebuild index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts all documents in a schema into a single index in the schema. Locks the index page catalog for write.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="physicalIndex"></param>
        private void RebuildIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIndex)
        {
            try
            {
                var physicalDocumentPageCatalog = _core.Documents.AcquireDocumentPageCatalog(transaction, physicalSchema, LockOperation.Read);

                //Clear out the existing index pages.
                if (Path.Exists(physicalIndex.GetPartitionPagesPath(physicalSchema)))
                {
                    _core.IO.DeletePath(transaction, physicalIndex.GetPartitionPagesPath(physicalSchema));
                }
                _core.IO.CreateDirectory(transaction, physicalIndex.GetPartitionPagesPath(physicalSchema));

                var physicalIndexPageMap = new Dictionary<uint, PhysicalIndexPages>();
                for (uint indexPartition = 0; indexPartition < physicalIndex.Partitions; indexPartition++)
                {
                    var physicalIndexPages = new PhysicalIndexPages();
                    physicalIndexPageMap.Add(indexPartition, physicalIndexPages);

                    _core.IO.PutPBuf(transaction, physicalIndex.GetPartitionPagesFileName
                        (physicalSchema, indexPartition), physicalIndexPages);
                }

                var childPool = _core.ThreadPool.Indexing.CreateChildPool<PhysicalDocumentPageCatalogItem>(_core.Settings.IndexingChildThreadPoolQueueDepth);

                var syncObjects = new object[physicalIndex.Partitions];
                for (uint indexPartition = 0; indexPartition < physicalIndex.Partitions; indexPartition++)
                {
                    syncObjects[indexPartition] = new object();
                }

                foreach (var physicalDocumentPageCatalogItem in physicalDocumentPageCatalog.Catalog)
                {
                    if (childPool.ExceptionOccurred())
                    {
                        break;
                    }

                    var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                    childPool.Enqueue(physicalDocumentPageCatalogItem, (threadPhysicalDocumentPageCatalogItem) =>
                    {
                        #region Thread.

                        try
                        {
                            transaction.EnsureActive();

                            var physicalDocumentPageMap = _core.Documents.AcquireDocumentPageMap(transaction,
                                physicalSchema, threadPhysicalDocumentPageCatalogItem.PageNumber, LockOperation.Read);

                            var documentPointers = physicalDocumentPageMap.DocumentIDs.Select(o => new DocumentPointer(threadPhysicalDocumentPageCatalogItem.PageNumber, o));

                            foreach (var documentPointer in documentPointers)
                            {
                                transaction.EnsureActive();

                                if (physicalSchema.DiskPath == null)
                                {
                                    throw new KbNullException($"Value should not be null: [{nameof(physicalSchema.DiskPath)}].");
                                }

                                var physicalDocument = _core.Documents.AcquireDocument(
                                    transaction, physicalSchema, documentPointer, LockOperation.Read);

                                try
                                {
                                    var documentField = physicalIndex.Attributes[0].Field;

                                    physicalDocument.Elements.TryGetValue(documentField.EnsureNotNull(), out string? value);

                                    uint indexPartition = physicalIndex.ComputePartition(value);
                                    string pageDiskPath = physicalIndex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                                    var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(transaction, pageDiskPath, LockOperation.Write);

                                    lock (syncObjects[indexPartition])
                                    {
                                        InsertDocumentIntoIndexPages(transaction, physicalIndex, physicalIndexPages, physicalDocument, documentPointer);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogManager.Error($"Failed to insert document into index for process id {transaction.ProcessId}, page number: {threadPhysicalDocumentPageCatalogItem.PageNumber}.", ex);
                                    throw;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.Error($"Failed to rebuild index by thread.", ex);
                            throw;
                        }

                        #endregion
                    });
                    ptThreadQueue?.StopAndAccumulate();
                }

                var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion, $"Index: {physicalIndex.Name}");
                childPool.WaitForCompletion();
                ptThreadCompletion?.StopAndAccumulate();

                for (uint indexPartition = 0; indexPartition < physicalIndex.Partitions; indexPartition++)
                {
                    _core.IO.PutPBuf(transaction, physicalIndex.GetPartitionPagesFileName(physicalSchema, indexPartition), physicalIndexPageMap[indexPartition]);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to rebuild index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        internal PhysicalIndexCatalog AcquireIndexCatalog(Transaction transaction, string schemaName, LockOperation intendedOperation)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, intendedOperation);
                return AcquireIndexCatalog(transaction, physicalSchema, intendedOperation);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to acquire index catalog for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal PhysicalIndexCatalog AcquireIndexCatalog(Transaction transaction,
            PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            try
            {
                var indexCatalog = _core.IO.GetJson<PhysicalIndexCatalog>(
                    transaction, physicalSchema.IndexCatalogFilePath(), intendedOperation);
                indexCatalog.DiskPath = physicalSchema.IndexCatalogFilePath();
                return indexCatalog;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to acquire index catalog for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}
