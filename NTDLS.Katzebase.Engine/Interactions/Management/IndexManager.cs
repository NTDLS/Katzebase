using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Engine.Threading;
using NTDLS.Katzebase.Exceptions;
using NTDLS.Katzebase.Payloads;
using NTDLS.Katzebase.Types;
using System.Text;
using static NTDLS.Katzebase.Engine.Indexes.Matching.IndexConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;
using static NTDLS.Katzebase.Engine.Trace.PerformanceTrace;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to indexes.
    /// </summary>
    public class IndexManager
    {
        private readonly Core _core;
        internal IndexQueryHandlers QueryHandlers { get; private set; }
        public IndexAPIHandlers APIHandlers { get; private set; }

        public IndexManager(Core core)
        {
            _core = core;
            try
            {
                QueryHandlers = new IndexQueryHandlers(core);
                APIHandlers = new IndexAPIHandlers(core);
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate indec manager.", ex);
                throw;
            }
        }

        internal void CreateIndex(Transaction transaction, string schemaName, KbIndex index, out Guid newId)
        {
            try
            {
                var physicalIndex = PhysicalIndex.FromClientPayload(index);

                physicalIndex.Id = Guid.NewGuid();
                physicalIndex.Created = DateTime.UtcNow;
                physicalIndex.Modfied = DateTime.UtcNow;

                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Write);

                if (indexCatalog.GetByName(index.Name) != null)
                {
                    throw new KbObjectAlreadysExistsException(index.Name);
                }

                indexCatalog.Add(physicalIndex);

                if (indexCatalog.DiskPath == null || physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
                }

                _core.IO.PutJson(transaction, indexCatalog.DiskPath, indexCatalog);

                RebuildIndex(transaction, physicalSchema, physicalIndex);

                newId = physicalIndex.Id;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to create index for process id {transaction.ProcessId}.", ex);
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
                    throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
                }

                var physicalIindex = indexCatalog.GetByName(indexName) ?? throw new KbObjectNotFoundException(indexName);

                var physicalIndexPageMap = new Dictionary<uint, PhysicalIndexPages>();
                var physicalIndexPageMapDistilledLeaves = new List<List<PhysicalIndexLeaf>>();

                double diskSize = 0;
                double decompressedSiskSize = 0;

                for (uint indexPartition = 0; indexPartition < physicalIindex.Partitions; indexPartition++)
                {
                    string pageDiskPath = physicalIindex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                    physicalIndexPageMap[indexPartition] = _core.IO.GetPBuf<PhysicalIndexPages>(transaction, pageDiskPath, LockOperation.Read);
                    diskSize += _core.IO.GetDecompressedSizeTracked(pageDiskPath);
                    decompressedSiskSize += new FileInfo(pageDiskPath).Length;
                    physicalIndexPageMapDistilledLeaves.Add(DistillIndexBaseNodes(physicalIndexPageMap[indexPartition].Root));
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
                builder.AppendLine($"    Name              : {physicalIindex.Name}");
                builder.AppendLine($"    Partitions        : {physicalIindex.Partitions}");
                builder.AppendLine($"    Id                : {physicalIindex.Id}");
                builder.AppendLine($"    Unique            : {physicalIindex.IsUnique}");
                builder.AppendLine($"    Created           : {physicalIindex.Created}");
                builder.AppendLine($"    Modified          : {physicalIindex.Modfied}");
                builder.AppendLine($"    Disk Path         : {physicalIindex.GetPartitionPagesPath(physicalSchema)}");
                builder.AppendLine($"    Pages Size        : {diskSize / 1024.0:N2}k");
                builder.AppendLine($"    Disk Size         : {decompressedSiskSize / 1024.0:N2}k");
                builder.AppendLine($"    Compression Ratio : {decompressedSiskSize / diskSize * 100.0:N2}");
                builder.AppendLine($"    Root Node Count   : {combinedNodes.Sum(o => o.Documents?.Count ?? 0):N0}");
                builder.AppendLine($"    Node Level Count  : {physicalIindex.Attributes.Count:N0}");
                builder.AppendLine($"    Min. Node Density : {minDocumentsPerNode:N0}");
                builder.AppendLine($"    Max. Node Density : {maxDocumentsPerNode:N0}" + (maxDocumentsPerNode == 1 ? " (unique)" : ""));
                builder.AppendLine($"    Avg. Node Density : {avgDocumentsPerNode:N2}");
                builder.AppendLine($"    Document Count    : {documentCount:N0}");
                builder.AppendLine($"    Selectivity Score : {selectivityScore:N4}%");

                builder.AppendLine("    Attributes {");
                foreach (var attri in physicalIindex.Attributes)
                {
                    builder.AppendLine($"        {attri.Field}");
                }
                builder.AppendLine("    }");
                builder.AppendLine("}");

                return builder.ToString();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to analyze index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void RebuildIndex(Transaction transaction, string schemaName, string indexName, uint newPartitionCount = 0)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Write);
                if (indexCatalog.DiskPath == null || physicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
                }

                var physicalIindex = indexCatalog.GetByName(indexName) ?? throw new KbObjectNotFoundException(indexName);

                if (newPartitionCount != 0)
                {
                    physicalIindex.Partitions = newPartitionCount;
                }

                RebuildIndex(transaction, physicalSchema, physicalIindex);

                physicalIindex.Modfied = DateTime.UtcNow;

                _core.IO.PutJson(transaction, indexCatalog.DiskPath, indexCatalog);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to rebuild index for process id {transaction.ProcessId}.", ex);
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
                    throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
                }

                var physicalIindex = indexCatalog.GetByName(indexName);
                if (physicalIindex != null)
                {
                    indexCatalog.Remove(physicalIindex);

                    if (Path.Exists(physicalIindex.GetPartitionPagesPath(physicalSchema)))
                    {
                        _core.IO.DeletePath(transaction, physicalIindex.GetPartitionPagesPath(physicalSchema));
                    }

                    _core.IO.PutJson(transaction, indexCatalog.DiskPath, indexCatalog);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to drop index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal Dictionary<uint, DocumentPointer> MatchConditionValuesDocuments(Transaction transaction, PhysicalSchema physicalSchema,
            IndexSelection indexSelection, ConditionSubset conditionSubset, KbInsensitiveDictionary<string> conditionValues)
        {
            var firstCondition = conditionSubset.Conditions.First();

            if (firstCondition.LogicalQualifier == LogicalQualifier.Equals)
            {
                //Yay, we have an "equals" condition so we can eliminate all but one partition.
                uint indexPartition = indexSelection.PhysicalIndex.ComputePartition(conditionValues.First().Value);
                string pageDiskPath = indexSelection.PhysicalIndex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(transaction, pageDiskPath, LockOperation.Read);
                return MatchDocuments(transaction, physicalIndexPages, indexSelection, conditionSubset, conditionValues);
            }
            else
            {
                //Unfortunately, we cant easily eliminate index partitions. Lets fire up some threads and scan all of the partitions.

                var ptThreadCreation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCreation);
                var threadParam = new MatchConditionValuesDocumentsThreadParam(transaction, indexSelection.PhysicalIndex, physicalSchema, indexSelection, conditionSubset, conditionValues);
                int threadCount = ThreadPoolHelper.CalculateThreadCount(_core, transaction, (int)indexSelection.PhysicalIndex.Partitions /*TODO: Use the total document count contained in the index*/);
                transaction.PT?.AddDescreteMetric(PerformanceTraceDescreteMetricType.ThreadCount, threadCount);
                var threadPool = ThreadPoolQueue<int?, MatchConditionValuesDocumentsThreadParam>
                    .CreateAndStart($"MatchConditionValuesDocuments:{transaction.ProcessId}", MatchConditionValuesDocumentsThreadProc, threadParam, threadCount, (int)indexSelection.PhysicalIndex.Partitions);
                ptThreadCreation?.StopAndAccumulate();

                for (int indexPartition = 0; indexPartition < indexSelection.PhysicalIndex.Partitions; indexPartition++)
                {
                    if (threadPool.HasException)
                    {
                        break;
                    }

                    var ptThreadQueue = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadQueue);
                    threadPool.EnqueueWorkItem(indexPartition);
                    ptThreadQueue?.StopAndAccumulate();
                }

                var ptThreadCompletion = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCompletion);
                threadPool.WaitForCompletion();
                ptThreadCompletion?.StopAndAccumulate();

                return threadParam.Results;
            }

        }

        private class MatchConditionValuesDocumentsThreadParam
        {
            public Transaction Transaction { get; set; }
            public PhysicalIndex PhysicalIindex { get; set; }
            public PhysicalSchema PhysicalSchema { get; set; }
            public IndexSelection IndexSelection { get; set; }
            public ConditionSubset ConditionSubset { get; set; }
            public KbInsensitiveDictionary<string> ConditionValues { get; set; }
            public Dictionary<uint, DocumentPointer> Results { get; set; } = new();

            public MatchConditionValuesDocumentsThreadParam(Transaction transaction, PhysicalIndex physicalIindex, PhysicalSchema physicalSchema, IndexSelection indexSelection, ConditionSubset conditionSubset, KbInsensitiveDictionary<string> conditionValues)
            {
                Transaction = transaction;
                PhysicalIindex = physicalIindex;
                PhysicalSchema = physicalSchema;
                IndexSelection = indexSelection;
                ConditionSubset = conditionSubset;
                ConditionValues = conditionValues;
            }
        }

        private void MatchConditionValuesDocumentsThreadProc(ThreadPoolQueue<int?, MatchConditionValuesDocumentsThreadParam> pool, MatchConditionValuesDocumentsThreadParam? param)
        {
            try
            {
                KbUtility.EnsureNotNull(param);

                while (pool.ContinueToProcessQueue)
                {
                    param.Transaction.EnsureActive();

                    var ptThreadDeQueue = param.Transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadDeQueue);
                    var indexPartition = pool.DequeueWorkItem();
                    ptThreadDeQueue?.StopAndAccumulate();
                    if (indexPartition == null)
                    {
                        continue;
                    }

                    string pageDiskPath = param.PhysicalIindex.GetPartitionPagesFileName(param.PhysicalSchema, (uint)indexPartition);
                    var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(param.Transaction, pageDiskPath, LockOperation.Write);

                    var results = MatchDocuments(param.Transaction, physicalIndexPages, param.IndexSelection, param.ConditionSubset, param.ConditionValues);
                    if (results.Any())
                    {
                        lock (param.Results)
                        {
                            foreach (var kvp in results)
                            {
                                param.Results.Add(kvp.Key, kvp.Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to delete from index by thread.", ex);
                throw;
            }
        }

        private Dictionary<uint, DocumentPointer> MatchDocuments(Transaction transaction, PhysicalIndexPages physicalIndexPages,
            IndexSelection indexSelection, ConditionSubset conditionSubset, KbInsensitiveDictionary<string> conditionValues)
        {
            try
            {
                List<PhysicalIndexLeaf> workingPhysicalIndexLeaves = new() { physicalIndexPages.Root };
                bool foundAnything = false;

                foreach (var attribute in indexSelection.PhysicalIndex.Attributes)
                {
                    KbUtility.EnsureNotNull(attribute.Field);
                    var conditionField = conditionSubset.Conditions.Where(o => o.Left.Value == attribute.Field.ToLowerInvariant()).FirstOrDefault();
                    if (conditionField == null)
                    {
                        //This happends when there is no condition on this index, this will be a partial match.
                        break;
                    }

                    if (conditionField.LogicalConnector == LogicalConnector.Or)
                    {
                        //TODO: Indexing only supports AND connectors, thats a performance problem.
                        break;
                    }

                    IEnumerable<PhysicalIndexLeaf>? foundLeaves = null;

                    var conditionValue = conditionValues[attribute.Field.ToLower()];

                    var ptIndexSeek = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexSearch);

                    if (conditionField.LogicalQualifier == LogicalQualifier.Equals)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchEqual(transaction, w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.NotEquals)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchEqual(transaction, w.Key, conditionValue) == false).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.GreaterThan)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchGreater(transaction, w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.GreaterThanOrEqual)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchGreaterOrEqual(transaction, w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.LessThan)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLesser(transaction, w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.LessThanOrEqual)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLesserOrEqual(transaction, w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.Like)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLike(transaction, w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.NotLike)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLike(transaction, w.Key, conditionValue) == false).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.Between)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchBetween(transaction, w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.NotBetween)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchBetween(transaction, w.Key, conditionField.Right.Value) == false).Select(s => s.Value));
                    else throw new KbNotImplementedException($"Logical qualifier has not been implemented for indexing: {conditionField.LogicalQualifier}");

                    ptIndexSeek?.StopAndAccumulate();

                    KbUtility.EnsureNotNull(foundLeaves);

                    if (foundLeaves.FirstOrDefault()?.Documents?.Any() == true) //We found documents, we are at the base of the index.
                    {
                        return foundLeaves.SelectMany(o => o.Documents ?? new List<PhysicalIndexEntry>()).ToDictionary(o => o.DocumentId, o => new DocumentPointer(o.PageNumber, o.DocumentId));
                    }

                    //Drop down to the next leve in the virtual tree we are building.
                    workingPhysicalIndexLeaves = new List<PhysicalIndexLeaf>();
                    workingPhysicalIndexLeaves.AddRange(foundLeaves);

                    if (foundAnything == false)
                    {
                        foundAnything = foundLeaves.Any();
                    }
                }

                if (foundAnything == false)
                {
                    return new Dictionary<uint, DocumentPointer>();
                }

                KbUtility.EnsureNotNull(workingPhysicalIndexLeaves);

                //This is an index scan.
                var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexDistillation);
                //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
                var resultingDocuments = DistillIndexLeaves(workingPhysicalIndexLeaves);
                ptIndexDistillation?.StopAndAccumulate();

                return resultingDocuments;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to match index documents for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal Dictionary<uint, DocumentPointer> MatchWorkingSchemaDocuments(Transaction transaction,
                    PhysicalSchema physicalSchema, IndexSelection indexSelection, ConditionSubset conditionSubset, string workingSchemaPrefix)
        {
            var firstCondition = conditionSubset.Conditions.First();

            if (firstCondition.LogicalQualifier == LogicalQualifier.Equals)
            {
                //Yay, we have an "equals" condition so we can eliminate all but one partition.

                var firstValue = firstCondition.Right.Value;

                KbUtility.EnsureNotNull(firstValue);
                uint indexPartition = indexSelection.PhysicalIndex.ComputePartition(firstValue);
                string pageDiskPath = indexSelection.PhysicalIndex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(transaction, pageDiskPath, LockOperation.Read);
                return MatchDocuments(transaction, physicalIndexPages, indexSelection, conditionSubset, workingSchemaPrefix);
            }
            else
            {
                //Unfortunately, we cant easily eliminate index partitions. Lets fire up some threads and scan all of the partitions.

                var ptThreadCreation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCreation);
                var threadParam = new MatchWorkingSchemaDocumentsThreadParam(transaction, indexSelection.PhysicalIndex, physicalSchema, indexSelection, conditionSubset, workingSchemaPrefix);
                int threadCount = ThreadPoolHelper.CalculateThreadCount(_core, transaction, (int)indexSelection.PhysicalIndex.Partitions /*TODO: Use the total document count contained in the index*/);
                transaction.PT?.AddDescreteMetric(PerformanceTraceDescreteMetricType.ThreadCount, threadCount);
                var threadPool = ThreadPoolQueue<int?, MatchWorkingSchemaDocumentsThreadParam>
                    .CreateAndStart($"MatchWorkingSchemaDocuments:{transaction.ProcessId}", MatchWorkingSchemaDocumentsThreadProc, threadParam, threadCount, (int)indexSelection.PhysicalIndex.Partitions);
                ptThreadCreation?.StopAndAccumulate();

                for (int indexPartition = 0; indexPartition < indexSelection.PhysicalIndex.Partitions; indexPartition++)
                {
                    if (threadPool.HasException)
                    {
                        break;
                    }

                    var ptThreadQueue = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadQueue);
                    threadPool.EnqueueWorkItem(indexPartition);
                    ptThreadQueue?.StopAndAccumulate();
                }

                var ptThreadCompletion = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCompletion);
                threadPool.WaitForCompletion();
                ptThreadCompletion?.StopAndAccumulate();

                return threadParam.Results;
            }
        }

        private class MatchWorkingSchemaDocumentsThreadParam
        {
            public Transaction Transaction { get; set; }
            public PhysicalIndex PhysicalIindex { get; set; }
            public PhysicalSchema PhysicalSchema { get; set; }
            public IndexSelection IndexSelection { get; set; }
            public ConditionSubset ConditionSubset { get; set; }
            public string WorkingSchemaPrefix { get; set; }
            public Dictionary<uint, DocumentPointer> Results { get; set; } = new();

            public MatchWorkingSchemaDocumentsThreadParam(Transaction transaction, PhysicalIndex physicalIindex, PhysicalSchema physicalSchema, IndexSelection indexSelection, ConditionSubset conditionSubset, string workingSchemaPrefix)
            {
                Transaction = transaction;
                PhysicalIindex = physicalIindex;
                PhysicalSchema = physicalSchema;
                IndexSelection = indexSelection;
                ConditionSubset = conditionSubset;
                WorkingSchemaPrefix = workingSchemaPrefix;
            }
        }

        private void MatchWorkingSchemaDocumentsThreadProc(ThreadPoolQueue<int?, MatchWorkingSchemaDocumentsThreadParam> pool, MatchWorkingSchemaDocumentsThreadParam? param)
        {
            try
            {
                KbUtility.EnsureNotNull(param);

                while (pool.ContinueToProcessQueue)
                {
                    param.Transaction.EnsureActive();

                    var ptThreadDeQueue = param.Transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadDeQueue);
                    var indexPartition = pool.DequeueWorkItem();
                    ptThreadDeQueue?.StopAndAccumulate();
                    if (indexPartition == null)
                    {
                        continue;
                    }

                    string pageDiskPath = param.PhysicalIindex.GetPartitionPagesFileName(param.PhysicalSchema, (uint)indexPartition);
                    var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(param.Transaction, pageDiskPath, LockOperation.Write);

                    var results = MatchDocuments(param.Transaction, physicalIndexPages, param.IndexSelection, param.ConditionSubset, param.WorkingSchemaPrefix);
                    if (results.Any())
                    {
                        lock (param.Results)
                        {
                            foreach (var kvp in results)
                            {
                                param.Results.Add(kvp.Key, kvp.Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to delete from index by thread.", ex);
                throw;
            }
        }

        /// <summary>
        /// Finds document IDs given a set of conditions.
        /// </summary>
        private Dictionary<uint, DocumentPointer> MatchDocuments(Transaction transaction,
                    PhysicalIndexPages physicalIndexPages, IndexSelection indexSelection, ConditionSubset conditionSubset, string workingSchemaPrefix)
        {
            try
            {
                List<PhysicalIndexLeaf> workingPhysicalIndexLeaves = new() { physicalIndexPages.Root };

                bool foundAnything = false;

                foreach (var attribute in indexSelection.PhysicalIndex.Attributes)
                {
                    KbUtility.EnsureNotNull(attribute.Field);
                    var conditionField = conditionSubset.Conditions
                        .Where(o => o.Left.Prefix == workingSchemaPrefix && o.Left.Value == attribute.Field.ToLowerInvariant()).FirstOrDefault();
                    if (conditionField == null)
                    {
                        //This happends when there is no condition on this index, this will be a partial match.
                        break;
                    }

                    if (conditionField.LogicalConnector == LogicalConnector.Or)
                    {
                        //TODO: Indexing only supports AND connectors, thats a performance problem.
                        break;
                    }

                    IEnumerable<PhysicalIndexLeaf>? foundLeaves = null;

                    var ptIndexSeek = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexSearch);

                    if (conditionField.LogicalQualifier == LogicalQualifier.Equals)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchEqual(transaction, w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.NotEquals)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchEqual(transaction, w.Key, conditionField.Right.Value) == false).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.GreaterThan)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchGreater(transaction, w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.GreaterThanOrEqual)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchGreaterOrEqual(transaction, w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.LessThan)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLesser(transaction, w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.LessThanOrEqual)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLesserOrEqual(transaction, w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.Like)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLike(transaction, w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.NotLike)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLike(transaction, w.Key, conditionField.Right.Value) == false).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.Between)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchBetween(transaction, w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.NotBetween)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchBetween(transaction, w.Key, conditionField.Right.Value) == false).Select(s => s.Value));
                    else throw new KbNotImplementedException($"Logical qualifier has not been implemented for indexing: {conditionField.LogicalQualifier}");

                    ptIndexSeek?.StopAndAccumulate();

                    KbUtility.EnsureNotNull(foundLeaves);

                    if (foundLeaves.FirstOrDefault()?.Documents?.Any() == true) //We found documents, we are at the base of the index.
                    {
                        return foundLeaves.SelectMany(o => o.Documents ?? new List<PhysicalIndexEntry>()).ToDictionary(o => o.DocumentId, o => new DocumentPointer(o.PageNumber, o.DocumentId));
                    }

                    //Drop down to the next leve in the virtual tree we are building.
                    workingPhysicalIndexLeaves = new List<PhysicalIndexLeaf>();
                    workingPhysicalIndexLeaves.AddRange(foundLeaves);

                    if (foundAnything == false)
                    {
                        foundAnything = foundLeaves.Any();
                    }
                }

                if (foundAnything == false)
                {
                    return new Dictionary<uint, DocumentPointer>();
                }

                KbUtility.EnsureNotNull(workingPhysicalIndexLeaves);

                //This is an index scan.
                var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexDistillation);
                //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
                var resultingDocuments = DistillIndexLeaves(workingPhysicalIndexLeaves);
                ptIndexDistillation?.StopAndAccumulate();

                return resultingDocuments;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to match index documents for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private List<PhysicalIndexLeaf> DistillIndexBaseNodes(List<PhysicalIndexLeaf> physicalIndexLeaves)
        {
            var result = new List<PhysicalIndexLeaf>();

            foreach (var leaf in physicalIndexLeaves)
            {
                result.AddRange(DistillIndexBaseNodes(leaf));
            }

            return result;
        }

        /// <summary>
        /// Traverse to the bottom of the index tree (from whatever starting point is passed in) and return a list of all nodes containing documents.
        /// </summary>
        /// <param name="indexEntires"></param>
        /// <returns></returns>
        private List<PhysicalIndexLeaf> DistillIndexBaseNodes(PhysicalIndexLeaf physicalIndexLeaf)
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

                    if (physicalIndexLeaf?.Documents?.Any() == true)
                    {
                        result.Add(physicalIndexLeaf);
                    }
                }

                if (physicalIndexLeaf?.Documents?.Any() == true)
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
                _core.Log.Write($"Failed to distill index base nodes.", ex);
                throw;
            }
        }

        private Dictionary<uint, DocumentPointer> DistillIndexLeaves(List<PhysicalIndexLeaf> physicalIndexLeaves)
        {
            var result = new List<DocumentPointer>();

            foreach (var leaf in physicalIndexLeaves)
            {
                result.AddRange(DistillIndexLeaves(leaf));
            }

            return result.ToDictionary(o => o.DocumentId, o => o);
        }

        /// <summary>
        /// Traverse to the bottom of the index tree (from whatever starting point is passed in) and return a list of all documentids.
        /// </summary>
        /// <param name="indexEntires"></param>
        /// <returns></returns>
        private List<DocumentPointer> DistillIndexLeaves(PhysicalIndexLeaf physicalIndexLeaf)
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

                    if (physicalIndexLeaf?.Documents?.Any() == true)
                    {
                        result.AddRange(physicalIndexLeaf.Documents.Select(o => new DocumentPointer(o.PageNumber, o.DocumentId)));
                    }
                }

                if (physicalIndexLeaf?.Documents?.Any() == true)
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
                _core.Log.Write($"Failed to distill index leaves.", ex);
                throw;
            }
        }

        internal PhysicalIndexCatalog AcquireIndexCatalog(Transaction transaction, string schemaName, LockOperation intendedOperation)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, intendedOperation);
                return AcquireIndexCatalog(transaction, physicalSchema, intendedOperation);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to acquire index catalog for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal PhysicalIndexCatalog AcquireIndexCatalog(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            try
            {
                var indexCatalog = _core.IO.GetJson<PhysicalIndexCatalog>(transaction, physicalSchema.IndexCatalogFilePath(), intendedOperation);
                indexCatalog.DiskPath = physicalSchema.IndexCatalogFilePath();
                return indexCatalog;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to acquire index catalog for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private List<string> GetIndexSearchTokens(Transaction transaction, PhysicalIndex physicalIindex, PhysicalDocument document)
        {
            try
            {
                var result = new List<string>();

                foreach (var indexAttribute in physicalIindex.Attributes)
                {
                    KbUtility.EnsureNotNull(indexAttribute.Field);
                    if (document.Elements.TryGetValue(indexAttribute.Field, out string? documentValue))
                    {
                        if (documentValue != null) //TODO: How do we handle indexed NULL values?
                        {
                            result.Add(documentValue.ToLower());
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to get index search tokens for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Finds the appropriate index page for a set of key values in the given index page catalog.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalIindex"></param>
        /// <param name="searchTokens"></param>
        /// <param name="indexPageCatalog"></param>
        /// <returns>A reference to a node in the suppliedIndexPageCatalog</returns>
        private IndexScanResult LocateExtentInGivenIndexPageCatalog(Transaction transaction, List<string> searchTokens, PhysicalIndexPages rootPhysicalIndexPages)
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

                    if (result.Leaf.Children.ContainsKey(token))
                    {
                        result.ExtentLevel++;
                        result.Leaf = result.Leaf.Children[token]; //Move one level lower in the extent tree.
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
                _core.Log.Write($"Failed to locate index extent for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

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

                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    if (listOfModifiedFields == null || physicalIindex.Attributes.Where(o => listOfModifiedFields.Contains(o.Field)).Any())
                    {
                        RemoveDocumentsFromIndex(transaction, physicalSchema, physicalIindex, documents.Select(o => o.Key));
                        InsertDocumentsIntoIndex(transaction, physicalSchema, physicalIindex, documents);
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to update document into indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocumentIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema, PhysicalDocument physicalDocument, DocumentPointer documentPointer)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, physicalDocument, documentPointer);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to insert document into indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocumentsIntoIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex, Dictionary<DocumentPointer, PhysicalDocument> documents)
        {
            try
            {
                foreach (var document in documents)
                {
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, document.Value, document.Key);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to insert document into indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }


        /// <summary>
        /// Inserts an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocumentsIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema, Dictionary<DocumentPointer, PhysicalDocument> documents)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                foreach (var document in documents)
                {
                    //Loop though each index in the schema.
                    foreach (var physicalIindex in indexCatalog.Collection)
                    {
                        InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, document.Value, document.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to insert document into indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index using the file name from the index object.
        /// </summary>
        private void InsertDocumentIntoIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex, PhysicalDocument document, DocumentPointer documentPointer)
        {
            try
            {
                var documentField = physicalIindex.Attributes[0].Field;
                KbUtility.EnsureNotNull(documentField);
                document.Elements.TryGetValue(documentField, out string? value);

                uint indexPartition = physicalIindex.ComputePartition(value);

                string pageDiskPath = physicalIindex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(transaction, pageDiskPath, LockOperation.Write);

                InsertDocumentIntoIndexPages(transaction, physicalIindex, physicalIndexPages, document, documentPointer);

                _core.IO.PutPBuf(transaction, pageDiskPath, physicalIndexPages);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to insert document into index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }


        /// <summary>
        /// Inserts an index entry for a single document into a single index using a long lived index page catalog.
        /// </summary>
        private void InsertDocumentIntoIndexPages(Transaction transaction, PhysicalIndex physicalIindex, PhysicalIndexPages physicalIndexPages, PhysicalDocument document, DocumentPointer documentPointer)
        {
            try
            {
                var searchTokens = GetIndexSearchTokens(transaction, physicalIindex, document);

                var indexScanResult = LocateExtentInGivenIndexPageCatalog(transaction, searchTokens, physicalIndexPages);

                //If we found a full match for all supplied key values - add the document to the leaf collection.
                if (indexScanResult.MatchType == IndexMatchType.Full)
                {
                    KbUtility.EnsureNotNull(indexScanResult.Leaf);

                    indexScanResult.Leaf.Documents ??= new List<PhysicalIndexEntry>();

                    if (physicalIindex.IsUnique && indexScanResult.Leaf.Documents.Count > 1)
                    {
                        string exceptionText = $"Duplicate key violation occurred for index [[{physicalIindex.Name}]. Values: {{{string.Join(",", searchTokens)}}}";
                        throw new KbDuplicateKeyViolationException(exceptionText);
                    }
                }
                else
                {
                    //If we didn't find a full match for all supplied key values, then create the tree and add the document to the
                    //  lowest leaf. Note that we are going to start creating the leaf level at the findResult.ExtentLevel. This is
                    //  because we may have a partial match and don't need to create the full tree.

                    for (int i = indexScanResult.ExtentLevel; i < searchTokens.Count; i++)
                    {
                        KbUtility.EnsureNotNull(indexScanResult?.Leaf);
                        indexScanResult.Leaf = indexScanResult.Leaf.AddNewLeaf(searchTokens[i]);
                    }

                    KbUtility.EnsureNotNull(indexScanResult?.Leaf);

                    indexScanResult.Leaf.Documents ??= new List<PhysicalIndexEntry>();
                }

                //Add the document to the lowest index extent.
                indexScanResult.Leaf.Documents.Add(new PhysicalIndexEntry(documentPointer.DocumentId, documentPointer.PageNumber));
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to insert document into index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #region Threading.

        private class RebuildIndexThreadParam
        {
            public Transaction Transaction { get; set; }
            public PhysicalSchema PhysicalSchema { get; set; }
            public PhysicalIndex PhysicalIindex { get; set; }
            public Dictionary<uint, PhysicalIndexPages> PhysicalIndexPageMap { get; set; }
            public object[] SyncObjects { get; private set; }

            public RebuildIndexThreadParam(Transaction transaction, PhysicalSchema physicalSchema,
                Dictionary<uint, PhysicalIndexPages> physicalIndexPageMap, PhysicalIndex physicalIindex, uint indexPartitions)
            {
                SyncObjects = new object[indexPartitions];

                for (uint indexPartition = 0; indexPartition < indexPartitions; indexPartition++)
                {
                    SyncObjects[indexPartition] = new object();
                }

                Transaction = transaction;
                PhysicalSchema = physicalSchema;
                PhysicalIindex = physicalIindex;
                PhysicalIndexPageMap = physicalIndexPageMap;
            }
        }

        #endregion

        private void RebuildIndexThreadProc(ThreadPoolQueue<DocumentPointer, RebuildIndexThreadParam> pool, RebuildIndexThreadParam? param)
        {
            try
            {
                KbUtility.EnsureNotNull(param);

                while (pool.ContinueToProcessQueue)
                {
                    param.Transaction.EnsureActive();

                    var ptThreadDeQueue = param.Transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadDeQueue);
                    var documentPointer = pool.DequeueWorkItem();
                    ptThreadDeQueue?.StopAndAccumulate();
                    if (documentPointer == null)
                    {
                        continue;
                    }

                    if (param.PhysicalSchema.DiskPath == null)
                    {
                        throw new KbNullException($"Value should not be null {nameof(param.PhysicalSchema.DiskPath)}.");
                    }

                    var physicalDocument = _core.Documents.AcquireDocument(param.Transaction, param.PhysicalSchema, documentPointer, LockOperation.Read);

                    try
                    {
                        var documentField = param.PhysicalIindex.Attributes[0].Field;
                        KbUtility.EnsureNotNull(documentField);
                        physicalDocument.Elements.TryGetValue(documentField, out string? value);

                        uint indexPartition = param.PhysicalIindex.ComputePartition(value);

                        lock (param.SyncObjects[indexPartition])
                        {
                            string pageDiskPath = param.PhysicalIindex.GetPartitionPagesFileName(param.PhysicalSchema, indexPartition);
                            var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(param.Transaction, pageDiskPath, LockOperation.Write);
                            InsertDocumentIntoIndexPages(param.Transaction, param.PhysicalIindex, physicalIndexPages, physicalDocument, documentPointer);
                        }
                    }
                    catch (Exception ex)
                    {
                        _core.Log.Write($"Failed to insert document into index for process id {param.Transaction.ProcessId}.", ex);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to rebuild index by thread.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts all documents in a schema into a single index in the schema. Locks the index page catalog for write.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="physicalIindex"></param>
        private void RebuildIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex)
        {
            try
            {
                var documentPointers = _core.Documents.AcquireDocumentPointers(transaction, physicalSchema, LockOperation.Read).ToList();

                //Clear out the existing index pages.
                if (Path.Exists(physicalIindex.GetPartitionPagesPath(physicalSchema)))
                {
                    _core.IO.DeletePath(transaction, physicalIindex.GetPartitionPagesPath(physicalSchema));
                }
                _core.IO.CreateDirectory(transaction, physicalIindex.GetPartitionPagesPath(physicalSchema));

                var physicalIndexPageMap = new Dictionary<uint, PhysicalIndexPages>();
                for (uint indexPartition = 0; indexPartition < physicalIindex.Partitions; indexPartition++)
                {
                    var physicalIndexPages = new PhysicalIndexPages();
                    physicalIndexPageMap.Add(indexPartition, physicalIndexPages);
                    _core.IO.PutPBuf(transaction, physicalIindex.GetPartitionPagesFileName(physicalSchema, indexPartition), physicalIndexPages);
                }

                var ptThreadCreation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCreation);
                var threadParam = new RebuildIndexThreadParam(transaction, physicalSchema, physicalIndexPageMap, physicalIindex, physicalIindex.Partitions);
                int threadCount = ThreadPoolHelper.CalculateThreadCount(_core, transaction, documentPointers.Count);
                transaction.PT?.AddDescreteMetric(PerformanceTraceDescreteMetricType.ThreadCount, threadCount);

                var threadPool = ThreadPoolQueue<DocumentPointer, RebuildIndexThreadParam>
                    .CreateAndStart($"RebuildIndex:{transaction.ProcessId}", RebuildIndexThreadProc, threadParam, threadCount, (int)physicalIindex.Partitions);

                ptThreadCreation?.StopAndAccumulate();

                foreach (var documentPointer in documentPointers)
                {
                    if (threadPool.HasException)
                    {
                        break;
                    }

                    var ptThreadQueue = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadQueue);
                    threadPool.EnqueueWorkItem(documentPointer);
                    ptThreadQueue?.StopAndAccumulate();
                }

                var ptThreadCompletion = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCompletion);
                threadPool.WaitForCompletion();
                ptThreadCompletion?.StopAndAccumulate();

                for (uint indexPartition = 0; indexPartition < physicalIindex.Partitions; indexPartition++)
                {
                    _core.IO.PutPBuf(transaction, physicalIindex.GetPartitionPagesFileName(physicalSchema, indexPartition), physicalIndexPageMap[indexPartition]);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to rebuild index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes a collection of document from all of the indexes on the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentPointer"></param>
        internal void RemoveDocumentsFromIndexes(Transaction transaction, PhysicalSchema physicalSchema, IEnumerable<DocumentPointer> documentPointers)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    RemoveDocumentsFromIndex(transaction, physicalSchema, physicalIindex, documentPointers);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to delete document from indexes for process id {transaction.ProcessId}.", ex);
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
                if (leaf.Documents?.Any() == true)
                {
                    foreach (var documentPointer in documentPointers)
                    {
                        totalDeletes += leaf?.Documents.RemoveAll(o => o.PageNumber == documentPointer.PageNumber && o.DocumentId == documentPointer.DocumentId) ?? 0;
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
                _core.Log.Write($"Failed to remove documents from index leaves.", ex);
                throw;
            }
        }



        /// <summary>
        /// Removes a collection of documents from an index. Locks the index page catalog for write.
        /// </summary>
        private void RemoveDocumentsFromIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex, IEnumerable<DocumentPointer> documentPointers)
        {
            try
            {
                bool useMultiThreadedIndexDeletion = true;

                //TODO: We need to determine how large this job is going to be and use threads when we have huge indexes.
                if (useMultiThreadedIndexDeletion == false)
                {
                    for (uint indexPartition = 0; indexPartition < physicalIindex.Partitions; indexPartition++)
                    {
                        string pageDiskPath = physicalIindex.GetPartitionPagesFileName(physicalSchema, indexPartition);
                        var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(transaction, pageDiskPath, LockOperation.Write);

                        if (RemoveDocumentsFromLeaves(physicalIndexPages.Root, documentPointers) > 0)
                        {
                            _core.IO.PutPBuf(transaction, pageDiskPath, physicalIndexPages);
                        }
                    }
                }
                else
                {
                    var ptThreadCreation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCreation);
                    var threadParam = new RemoveDocumentsFromIndexThreadParam(transaction, physicalIindex, physicalSchema, documentPointers);
                    int threadCount = ThreadPoolHelper.CalculateThreadCount(_core, transaction, (int)physicalIindex.Partitions /*TODO: Use the total document count contained in the index*/);
                    transaction.PT?.AddDescreteMetric(PerformanceTraceDescreteMetricType.ThreadCount, threadCount);
                    var threadPool = ThreadPoolQueue<int?, RemoveDocumentsFromIndexThreadParam>
                        .CreateAndStart($"RemoveDocumentsFromIndex:{transaction.ProcessId}", RemoveDocumentsFromIndexThreadProc, threadParam, threadCount, (int)physicalIindex.Partitions);
                    ptThreadCreation?.StopAndAccumulate();

                    for (int indexPartition = 0; indexPartition < physicalIindex.Partitions; indexPartition++)
                    {
                        if (threadPool.HasException)
                        {
                            break;
                        }

                        var ptThreadQueue = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadQueue);
                        threadPool.EnqueueWorkItem(indexPartition);
                        ptThreadQueue?.StopAndAccumulate();
                    }

                    var ptThreadCompletion = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCompletion);
                    threadPool.WaitForCompletion();
                    ptThreadCompletion?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to remove documents from index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private class RemoveDocumentsFromIndexThreadParam
        {
            public Transaction Transaction { get; set; }
            public PhysicalIndex PhysicalIindex { get; set; }
            public PhysicalSchema PhysicalSchema { get; set; }
            public IEnumerable<DocumentPointer> DocumentPointers { get; set; }

            public RemoveDocumentsFromIndexThreadParam(Transaction transaction, PhysicalIndex physicalIindex, PhysicalSchema physicalSchema, IEnumerable<DocumentPointer> documentPointers)
            {
                Transaction = transaction;
                PhysicalIindex = physicalIindex;
                PhysicalSchema = physicalSchema;
                DocumentPointers = documentPointers;
            }
        }

        private void RemoveDocumentsFromIndexThreadProc(ThreadPoolQueue<int?, RemoveDocumentsFromIndexThreadParam> pool, RemoveDocumentsFromIndexThreadParam? param)
        {
            try
            {
                KbUtility.EnsureNotNull(param);

                while (pool.ContinueToProcessQueue)
                {
                    param.Transaction.EnsureActive();

                    var ptThreadDeQueue = param.Transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadDeQueue);
                    var indexPartition = pool.DequeueWorkItem();
                    ptThreadDeQueue?.StopAndAccumulate();
                    if (indexPartition == null)
                    {
                        continue;
                    }

                    string pageDiskPath = param.PhysicalIindex.GetPartitionPagesFileName(param.PhysicalSchema, (uint)indexPartition);
                    var physicalIndexPages = _core.IO.GetPBuf<PhysicalIndexPages>(param.Transaction, pageDiskPath, LockOperation.Write);

                    if (RemoveDocumentsFromLeaves(physicalIndexPages.Root, param.DocumentPointers) > 0)
                    {
                        _core.IO.PutPBuf(param.Transaction, pageDiskPath, physicalIndexPages);
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to delete from index by thread.", ex);
                throw;
            }
        }
    }
}
