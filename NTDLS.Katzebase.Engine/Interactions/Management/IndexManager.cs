using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Library;

using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Engine.QueryProcessing;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Engine.Threading.PoolingParameters;
using System.Text;
using static NTDLS.Katzebase.Engine.Indexes.Matching.IndexConstants;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;
using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.Parsers.Query;
using static NTDLS.Katzebase.Parsers.Constants;
using NTDLS.Katzebase.Parsers.Indexes.Matching;


namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to indexes.
    /// </summary>
    public class IndexManager<TData> where TData : IStringable
    {
        private readonly EngineCore<TData> _core;
        internal IndexQueryHandlers<TData> QueryHandlers { get; private set; }
        public IndexAPIHandlers<TData> APIHandlers { get; private set; }

        internal IndexManager(EngineCore<TData> core)
        {
            _core = core;
            try
            {
                QueryHandlers = new IndexQueryHandlers<TData>(core);
                APIHandlers = new IndexAPIHandlers<TData>(core);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate index manager.", ex);
                throw;
            }
        }

        #region Create / Analyze / Rebuild / Drop.

        internal void CreateIndex(Transaction<TData> transaction, string schemaName, KbIndex index, out Guid newId)
        {
            try
            {
                var physicalIndex = PhysicalIndex<TData>.FromClientPayload(index);

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

        internal string AnalyzeIndex(Transaction<TData> transaction, string schemaName, string indexName)
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

                var physicalIndexPageMap = new Dictionary<uint, PhysicalIndexPages<TData>>();
                var physicalIndexPageMapDistilledLeaves = new List<List<PhysicalIndexLeaf<TData>>>();

                double diskSize = 0;
                double decompressedSiskSize = 0;

                int rootNodes = 0;

                for (uint indexPartition = 0; indexPartition < physicalIndex.Partitions; indexPartition++)
                {
                    string pageDiskPath = physicalIndex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                    physicalIndexPageMap[indexPartition] = _core.IO.GetPBuf<PhysicalIndexPages<TData>>(transaction, pageDiskPath, LockOperation.Read);
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

        internal void DropIndex(Transaction<TData> transaction, string schemaName, string indexName)
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
        /// <param name="keyValues">For JOIN operations, contains the values of the joining document.</param>
        /// <returns></returns>
        internal Dictionary<uint, DocumentPointer<TData>> MatchSchemaDocumentsByConditionsClause(
                    PhysicalSchema<TData> physicalSchema, IndexingConditionOptimization<TData> optimization,
                    PreparedQuery<TData> query, string workingSchemaPrefix, KbInsensitiveDictionary<TData?>? keyValues = null)
        {
            Dictionary<uint, DocumentPointer<TData>>? accumulatedResults = null;

            var ptIndexSearch = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.IndexSearch, $"Schema: {workingSchemaPrefix}");

            //We aggregate the values for the entries into the ConditionGroup.IndexLookup,
            //  which contains all of the values for all entries in the group.
            //  For this reason, we do not perform index lookups on individual condition entries.
            foreach (var group in optimization.Conditions.Collection.OfType<ConditionGroup<TData>>().Where(group => group.IndexLookup != null))
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

        private Dictionary<uint, DocumentPointer<TData>> MatchSchemaDocumentsByConditionsClauseRecursive(
            PhysicalSchema<TData> physicalSchema, IndexingConditionOptimization<TData> optimization, ConditionGroup<TData> givenConditionGroup,
            PreparedQuery<TData> query, string workingSchemaPrefix, KbInsensitiveDictionary<TData?>? keyValues = null)
        {
            var thisGroupResults = MatchSchemaDocumentsByIndexingConditionLookup(optimization.Transaction,
                query, givenConditionGroup.IndexLookup.EnsureNotNull(), physicalSchema, workingSchemaPrefix, keyValues);

            foreach (var group in givenConditionGroup.Collection.OfType<ConditionGroup<TData>>().Where(o => o.IndexLookup != null))
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

        private Dictionary<uint, DocumentPointer<TData>> MatchSchemaDocumentsByIndexingConditionLookup(Transaction<TData> transaction, PreparedQuery<TData> query,
            IndexingConditionLookup<TData> indexLookup, PhysicalSchema<TData> physicalSchema, string workingSchemaPrefix, KbInsensitiveDictionary<TData?>? keyValues)
        {
            Dictionary<uint, DocumentPointer<TData>>? accumulatedResults = null;

            try
            {
                var conditionEntries = indexLookup.AttributeConditionSets[indexLookup.IndexSelection.PhysicalIndex.Attributes[0].Field.EnsureNotNull()];

                foreach (var condition in conditionEntries)
                {
                    List<uint> indexPartitions = new();

                    if (condition.Qualifier == LogicalQualifier.Equals)
                    {
                        //For join operations, check the keyValues for the raw value to lookup.
                        if (keyValues?.TryGetValue(condition.Right.Value.ToT<string>(), out TData? keyValue) != true)
                        {
                            if (condition.Right is QueryFieldCollapsedValue<TData> collapsedValue)
                            {
                                keyValue = collapsedValue.Value;
                            }
                            else
                            {
                                keyValue = (TData)condition.Right.CollapseScalerQueryField(transaction, query, query.SelectFields, keyValues ?? new())?.ToLowerInvariant();
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

                    var operation = new MatchSchemaDocumentsByConditionsOperation<TData>(transaction, query, indexLookup, physicalSchema, workingSchemaPrefix, condition, keyValues);

                    if (indexPartitions.Count > 1)
                    {
                        var queue = _core.ThreadPool.Indexing.CreateChildQueue<MatchSchemaDocumentsByConditionsOperation<TData>.Instance>(_core.Settings.IndexingOperationThreadPoolQueueDepth);

                        foreach (var indexPartition in indexPartitions)
                        {
                            var parameter = new MatchSchemaDocumentsByConditionsOperation<TData>.Instance(operation, indexPartition);

                            var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                            queue.Enqueue(parameter, MatchSchemaDocumentsByIndexingConditionLookupThread/*, (QueueItemState<MatchSchemaDocumentsByConditionsOperation.Parameter> o) =>
                        {
                            LogManager.Information($"Indexing:CompletionTime: {o.CompletionTime?.TotalMilliseconds:n0}.");
                        }*/);
                            ptThreadQueue?.StopAndAccumulate();
                        }

                        var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion, $"Index: {indexLookup.IndexSelection.PhysicalIndex.Name}");
                        queue.WaitForCompletion();
                        ptThreadCompletion?.StopAndAccumulate();

                        var ptDocumentPointerIntersect = transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                        accumulatedResults = accumulatedResults.IntersectWith(operation.ThreadResults);
                        ptDocumentPointerIntersect?.StopAndAccumulate();

                        //LogManager.Debug($"Depth: root, Count: {operation.ThreadResults.Count}, Total: {accumulatedResults.Count}");
                        if (accumulatedResults.Count == 0)
                        {
                            break; //Condition eliminated all possible results on this level.
                        }
                    }
                    else
                    {
                        //No need for additional threads, its just a single partition.
                        MatchSchemaDocumentsByIndexingConditionLookupThread(new MatchSchemaDocumentsByConditionsOperation<TData>.Instance(operation, indexPartitions.First()));

                        var ptDocumentPointerIntersect = transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                        accumulatedResults = accumulatedResults.IntersectWith(operation.ThreadResults);
                        ptDocumentPointerIntersect?.StopAndAccumulate();

                        //LogManager.Debug($"Depth: root, Count: {operation.ThreadResults.Count}, Total: {accumulatedResults.Count}");
                        if (accumulatedResults.Count == 0)
                        {
                            break; //Condition eliminated all possible results on this level.
                        }
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

        private void MatchSchemaDocumentsByIndexingConditionLookupThread(MatchSchemaDocumentsByConditionsOperation<TData>.Instance instance)
        {
            Thread.CurrentThread.Name = $"@Thread_{instance.Operation.PhysicalSchema.Name}:{instance.IndexPartition}";

            try
            {
                Dictionary<uint, DocumentPointer<TData>> threadResults = new();

                string pageDiskPath = instance.Operation.Lookup.IndexSelection.PhysicalIndex.GetPartitionPagesFileName(instance.Operation.PhysicalSchema, instance.IndexPartition);
                var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages<TData>>(instance.Operation.Transaction, pageDiskPath, LockOperation.Read);

                List<PhysicalIndexLeaf<TData>> workingPhysicalIndexLeaves = [physicalIndexPages.Root];
                if (workingPhysicalIndexLeaves.Count > 0)
                {
                    //First process the condition at the AttributeDepth that was passed in.
                    workingPhysicalIndexLeaves = MatchIndexLeaves(instance.Operation.Transaction, instance.Operation.Query,
                        instance.Operation.Condition, workingPhysicalIndexLeaves, instance.Operation.Query.Conditions.FieldCollection, instance.Operation.KeyValues);

                    if (instance.Operation.Lookup.IndexSelection.PhysicalIndex.Attributes.Count == 1)
                    {
                        //The index only has one attribute, so we are at the base where the document pointers are.
                        //(workingPhysicalIndexLeaves.FirstOrDefault()?.Documents?.Count > 0) //We found documents, we are at the base of the index.
                        var ptIndexDistillation = instance.Operation.Transaction.Instrumentation.CreateToken(PerformanceCounter.IndexDistillation);
                        threadResults = DistillIndexLeaves(workingPhysicalIndexLeaves);
                        ptIndexDistillation?.StopAndAccumulate();

                    }
                    else if (workingPhysicalIndexLeaves.Count > 0)
                    {
                        //Further, recursively, process additional compound index attribute condition matches.
                        threadResults = MatchSchemaDocumentsByIndexingConditionLookupRecursive(instance, 1, workingPhysicalIndexLeaves);
                    }
                }

                if (threadResults.Count > 0)
                {
                    var ptDocumentPointerUnion = instance.Operation.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerUnion);
                    lock (instance.Operation.ThreadResults)
                    {
                        instance.Operation.ThreadResults.UnionWith(threadResults);
                    }
                    ptDocumentPointerUnion?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to match index by thread.", ex);
                throw;
            }
        }

        private static Dictionary<uint, DocumentPointer<TData>> MatchSchemaDocumentsByIndexingConditionLookupRecursive(
            MatchSchemaDocumentsByConditionsOperation<TData>.Instance instance, int attributeDepth, List<PhysicalIndexLeaf<TData>> workingPhysicalIndexLeaves)
        {
            Dictionary<uint, DocumentPointer<TData>>? results = null;

            var conditionSet = instance.Operation.Lookup.AttributeConditionSets[
                instance.Operation.Lookup.IndexSelection.PhysicalIndex.Attributes[attributeDepth].Field.EnsureNotNull()];

            foreach (var condition in conditionSet)
            {
                var partitionResults = MatchIndexLeaves(instance.Operation.Transaction, instance.Operation.Query,
                    condition, workingPhysicalIndexLeaves, instance.Operation.Query.Conditions.FieldCollection, instance.Operation.KeyValues);

                if (attributeDepth == instance.Operation.Lookup.AttributeConditionSets.Count - 1)
                {
                    //This is the bottom of the condition tree, as low as we can go in this index given the fields we have, so just distill the leaves.

                    var ptIndexDistillation = instance.Operation.Transaction.Instrumentation.CreateToken(PerformanceCounter.IndexDistillation);
                    var partialResults = DistillIndexLeaves(partitionResults);
                    ptIndexDistillation?.StopAndAccumulate();

                    var ptDocumentPointerIntersect = instance.Operation.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                    results = results.IntersectWith(partialResults);
                    ptDocumentPointerIntersect?.StopAndAccumulate();

                    //LogManager.Debug($"Partition: {parameter.IndexPartition}, Depth: {attributeDepth}, Count: {partialResults.Count}, Total: {results.Count}");
                    if (results.Count == 0)
                    {
                        break; //Condition eliminated all possible results on this level.
                    }
                }
                else if (attributeDepth < instance.Operation.Lookup.AttributeConditionSets.Count - 1)
                {
                    //Further, recursively, process additional compound index attribute condition matches.
                    var partialResults = MatchSchemaDocumentsByIndexingConditionLookupRecursive(instance, attributeDepth + 1, partitionResults);

                    var ptDocumentPointerIntersect = instance.Operation.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
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

        private static List<PhysicalIndexLeaf<TData>> MatchIndexLeaves(Transaction<TData> transaction, PreparedQuery<TData> query, ConditionEntry<TData> condition,
            List<PhysicalIndexLeaf<TData>> workingPhysicalIndexLeaves, QueryFieldCollection<TData> fieldCollection, KbInsensitiveDictionary<TData?>? auxiliaryFields)
        {
            //For join operations, check the keyValues for the raw value to lookup.
            if (auxiliaryFields?.TryGetValue(condition.Right.Value.ToT<string>(), out TData? keyValue) != true)
            {
                if (condition.Right is QueryFieldCollapsedValue<TData> collapsedValue)
                {
                    keyValue = collapsedValue.Value;
                }
                else
                {
                    keyValue = (TData)condition.Right.CollapseScalerQueryField(transaction, query, fieldCollection, auxiliaryFields ?? new())?.ToLowerInvariant();
                }
            }

            return condition.Qualifier switch
            {
                LogicalQualifier.Equals => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry<TData>.IsMatchEqual(transaction, w.Key, keyValue.ToT<string>()) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.NotEquals => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry<TData>.IsMatchEqual(transaction, w.Key, keyValue.ToT<string>()) == false)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.GreaterThan => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry<TData>.IsMatchGreater(transaction, w.Key, keyValue.ToT<string>()) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.GreaterThanOrEqual => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry<TData>.IsMatchGreaterOrEqual(transaction, w.Key, keyValue.ToT<string>()) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.LessThan => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry<TData>.IsMatchLesser(transaction, w.Key, keyValue.ToT<string>()) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.LessThanOrEqual => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry<TData>.IsMatchLesserOrEqual(transaction, w.Key, keyValue.ToT<string>()) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.Like => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry<TData>.IsMatchLike(transaction, w.Key, keyValue.ToT<string>()) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.NotLike => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry<TData>.IsMatchLike(transaction, w.Key, keyValue.ToT<string>()) == false)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.Between => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry<TData>.IsMatchBetween(transaction, w.Key, keyValue.ToT<string>()) == true)
                                        .Select(s => s.Value)).ToList(),
                LogicalQualifier.NotBetween => workingPhysicalIndexLeaves
                                        .SelectMany(o => o.Children
                                        .Where(w => ConditionEntry<TData>.IsMatchBetween(transaction, w.Key, keyValue.ToT<string>()) == false)
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
        private static List<PhysicalIndexLeaf<TData>> DistillIndexBaseNodes(PhysicalIndexLeaf<TData> physicalIndexLeaf)
        {
            try
            {
                var result = new List<PhysicalIndexLeaf<TData>>();

                void DistillIndexBaseNodesRecursive(PhysicalIndexLeaf<TData> physicalIndexLeaf)
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

        private static Dictionary<uint, DocumentPointer<TData>> DistillIndexLeaves(List<PhysicalIndexLeaf<TData>> physicalIndexLeaves)
        {
            var result = new List<DocumentPointer<TData>>();

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
        private static List<DocumentPointer<TData>> DistillIndexLeaves(PhysicalIndexLeaf<TData> physicalIndexLeaf)
        {
            try
            {
                var result = new List<DocumentPointer<TData>>();

                void DistillIndexLeavesRecursive(PhysicalIndexLeaf<TData> physicalIndexLeaf)
                {
                    foreach (var child in physicalIndexLeaf.Children)
                    {
                        DistillIndexLeavesRecursive(child.Value);
                    }

                    if (physicalIndexLeaf?.Documents?.Count > 0)
                    {
                        result.AddRange(physicalIndexLeaf.Documents.Select(o => new DocumentPointer<TData>(o.PageNumber, o.DocumentId)));
                    }
                }

                if (physicalIndexLeaf?.Documents?.Count > 0)
                {
                    result.AddRange(physicalIndexLeaf.Documents.Select(o => new DocumentPointer<TData>(o.PageNumber, o.DocumentId)));
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
        internal void InsertDocumentIntoIndexes(Transaction<TData> transaction,
            PhysicalSchema<TData> physicalSchema, PhysicalDocument<TData> physicalDocument, DocumentPointer<TData> documentPointer)
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
        internal void InsertDocumentsIntoIndex(Transaction<TData> transaction,
            PhysicalSchema<TData> physicalSchema, PhysicalIndex<TData> physicalIndex, Dictionary<DocumentPointer<TData>, PhysicalDocument<TData>> documents)
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
        internal void InsertDocumentsIntoIndexes(Transaction<TData> transaction,
            PhysicalSchema<TData> physicalSchema, Dictionary<DocumentPointer<TData>, PhysicalDocument<TData>> documents)
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
        private void InsertDocumentIntoIndex(Transaction<TData> transaction,
            PhysicalSchema<TData> physicalSchema, PhysicalIndex<TData> physicalIndex, PhysicalDocument<TData> document, DocumentPointer<TData> documentPointer)
        {
            try
            {
                var documentField = physicalIndex.Attributes[0].Field;
                document.Elements.TryGetValue(documentField.EnsureNotNull(), out TData? value);

                uint indexPartition = physicalIndex.ComputePartition(value);

                string pageDiskPath = physicalIndex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages<TData>>(transaction, pageDiskPath, LockOperation.Write);

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
        private static void InsertDocumentIntoIndexPages(Transaction<TData> transaction, PhysicalIndex<TData> physicalIndex, PhysicalIndexPages<TData> physicalIndexPages, PhysicalDocument<TData> document, DocumentPointer<TData> documentPointer)
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
                        indexScanResult.Leaf = indexScanResult.Leaf.EnsureNotNull().AddNewLeaf(searchTokens[i].CastToT<TData>(EngineCore<TData>.StrCast));
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

        private static List<string> GetIndexSearchTokens(Transaction<TData> transaction, PhysicalIndex<TData> physicalIndex, PhysicalDocument<TData> document)
        {
            try
            {
                var result = new List<string>();

                foreach (var indexAttribute in physicalIndex.Attributes)
                {
                    if (document.Elements.TryGetValue(indexAttribute.Field.EnsureNotNull(), out TData? documentValue))
                    {
                        if (documentValue != null) //TODO: How do we handle indexed NULL values?
                        {
                            result.Add(documentValue.ToLowerInvariant().GetKey());
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
        private static IndexScanResult<TData> LocateExtentInGivenIndexPageCatalog(
            Transaction<TData> transaction, List<string> searchTokens, PhysicalIndexPages<TData> rootPhysicalIndexPages)
        {
            try
            {
                var physicalIndexPages = rootPhysicalIndexPages;

                var result = new IndexScanResult<TData>()
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

                    if (result.Leaf.Children.TryGetValue(token, out PhysicalIndexLeaf<TData>? value))
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
        internal void UpdateDocumentsIntoIndexes(Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema,
            Dictionary<DocumentPointer<TData>, PhysicalDocument<TData>> documents, IEnumerable<string>? listOfModifiedFields)
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
        internal void RemoveDocumentsFromIndexes(Transaction<TData> transaction,
            PhysicalSchema<TData> physicalSchema, IEnumerable<DocumentPointer<TData>> documentPointers)
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

        private long RemoveDocumentsFromLeaves(PhysicalIndexLeaf<TData> leaf, IEnumerable<DocumentPointer<TData>> documentPointers)
        {
            return RemoveDocumentsFromLeaves(leaf, documentPointers, documentPointers.Count());
        }

        private long RemoveDocumentsFromLeaves(PhysicalIndexLeaf<TData> leaf, IEnumerable<DocumentPointer<TData>> documentPointers, long maxCount)
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
        private void RemoveDocumentsFromIndex(Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema,
            PhysicalIndex<TData> physicalIndex, IEnumerable<DocumentPointer<TData>> documentPointers)
        {
            try
            {
                bool useMultiThreadedIndexDeletion = true;

                //TODO: We need to determine how large this job is going to be and use threads when we have huge indexes.
                if (useMultiThreadedIndexDeletion == false)
                {
                    for (uint indexPartition = 0; indexPartition < physicalIndex.Partitions; indexPartition++)
                    {
                        string pageDiskPath = physicalIndex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                        var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages<TData>>(transaction, pageDiskPath, LockOperation.Write);

                        if (RemoveDocumentsFromLeaves(physicalIndexPages.Root, documentPointers) > 0)
                        {
                            _core.IO.PutPBuf(transaction, pageDiskPath, physicalIndexPages);
                        }
                    }
                }
                else
                {
                    var queue = _core.ThreadPool.Indexing.CreateChildQueue<RemoveDocumentsFromIndexThreadOperation<TData>.Instance>(_core.Settings.IndexingOperationThreadPoolQueueDepth);
                    var operation = new RemoveDocumentsFromIndexThreadOperation<TData>(transaction, physicalIndex, physicalSchema, documentPointers);

                    for (int indexPartition = 0; indexPartition < physicalIndex.Partitions; indexPartition++)
                    {
                        if (queue.ExceptionOccurred())
                        {
                            break;
                        }

                        var instance = new RemoveDocumentsFromIndexThreadOperation<TData>.Instance(operation, indexPartition);

                        var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                        queue.Enqueue(instance, RemoveDocumentsFromIndexThreadWorker);
                        ptThreadQueue?.StopAndAccumulate();
                    }

                    var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion, $"Index: {physicalIndex.Name}");
                    queue.WaitForCompletion();
                    ptThreadCompletion?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to remove documents from index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private void RemoveDocumentsFromIndexThreadWorker(RemoveDocumentsFromIndexThreadOperation<TData>.Instance instance)
        {
            try
            {
                instance.Operation.Transaction.EnsureActive();

                string pageDiskPath = instance.Operation.PhysicalIndex.GetPartitionPagesFileName(
                    instance.Operation.PhysicalSchema, (uint)instance.IndexPartition);

                var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages<TData>>(instance.Operation.Transaction, pageDiskPath, LockOperation.Write);

                if (RemoveDocumentsFromLeaves(physicalIndexPages.Root, instance.Operation.DocumentPointers) > 0)
                {
                    _core.IO.PutPBuf(instance.Operation.Transaction, pageDiskPath, physicalIndexPages);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to delete from index by thread.", ex);
                throw;
            }
        }

        #endregion

        #region Rebuild Index.

        internal void RebuildIndex(Transaction<TData> transaction, string schemaName, string indexName, uint newPartitionCount = 0)
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

        private void RebuildIndexThreadWorker(RebuildIndexOperation<TData>.Instance instance)
        {
            try
            {
                instance.Operation.Transaction.EnsureActive();

                var physicalDocumentPageMap = _core.Documents.AcquireDocumentPageMap(instance.Operation.Transaction,
                    instance.Operation.PhysicalSchema, instance.PageNumber, LockOperation.Read);

                var documentPointers = physicalDocumentPageMap.DocumentIDs.Select(o => new DocumentPointer<TData>(instance.PageNumber, o));

                foreach (var documentPointer in documentPointers)
                {
                    instance.Operation.Transaction.EnsureActive();

                    if (instance.Operation.PhysicalSchema.DiskPath == null)
                    {
                        throw new KbNullException($"Value should not be null: [{nameof(instance.Operation.PhysicalSchema.DiskPath)}].");
                    }

                    var physicalDocument = _core.Documents.AcquireDocument(
                        instance.Operation.Transaction, instance.Operation.PhysicalSchema, documentPointer, LockOperation.Read);

                    try
                    {
                        var documentField = instance.Operation.PhysicalIndex.Attributes[0].Field;
                        physicalDocument.Elements.TryGetValue(documentField.EnsureNotNull(), out TData? value);

                        uint indexPartition = instance.Operation.PhysicalIndex.ComputePartition(value);

                        string pageDiskPath = instance.Operation.PhysicalIndex.GetPartitionPagesFileName(
                            instance.Operation.PhysicalSchema, indexPartition);

                        var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages<TData>>(
                            instance.Operation.Transaction, pageDiskPath, LockOperation.Write);

                        lock (instance.Operation.SyncObjects[indexPartition])
                        {
                            InsertDocumentIntoIndexPages(instance.Operation.Transaction,
                                instance.Operation.PhysicalIndex, physicalIndexPages, physicalDocument, documentPointer);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to insert document into index for process id {instance.Operation.Transaction.ProcessId}.", ex);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to rebuild index by thread.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts all documents in a schema into a single index in the schema. Locks the index page catalog for write.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="physicalIndex"></param>
        private void RebuildIndex(Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema, PhysicalIndex<TData> physicalIndex)
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

                var physicalIndexPageMap = new Dictionary<uint, PhysicalIndexPages<TData>>();
                for (uint indexPartition = 0; indexPartition < physicalIndex.Partitions; indexPartition++)
                {
                    var physicalIndexPages = new PhysicalIndexPages<TData>();
                    physicalIndexPageMap.Add(indexPartition, physicalIndexPages);

                    _core.IO.PutPBuf(transaction, physicalIndex.GetPartitionPagesFileName
                        (physicalSchema, indexPartition), physicalIndexPages);
                }

                var queue = _core.ThreadPool.Indexing.CreateChildQueue<RebuildIndexOperation<TData>.Instance>(_core.Settings.IndexingOperationThreadPoolQueueDepth);

                var operation = new RebuildIndexOperation<TData>(
                    transaction, physicalSchema, physicalIndexPageMap, physicalIndex, physicalIndex.Partitions);

                foreach (var physicalDocumentPageCatalogItem in physicalDocumentPageCatalog.Catalog)
                {
                    if (queue.ExceptionOccurred())
                    {
                        break;
                    }

                    var parameter = new RebuildIndexOperation<TData>.Instance(operation, physicalDocumentPageCatalogItem.PageNumber);

                    var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                    queue.Enqueue(parameter, RebuildIndexThreadWorker);
                    ptThreadQueue?.StopAndAccumulate();
                }

                var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion, $"Index: {physicalIndex.Name}");
                queue.WaitForCompletion();
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

        internal PhysicalIndexCatalog<TData> AcquireIndexCatalog(Transaction<TData> transaction, string schemaName, LockOperation intendedOperation)
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

        internal PhysicalIndexCatalog<TData> AcquireIndexCatalog(Transaction<TData> transaction,
            PhysicalSchema<TData> physicalSchema, LockOperation intendedOperation)
        {
            try
            {
                var indexCatalog = _core.IO.GetJson<PhysicalIndexCatalog<TData>>(
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
