using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Instrumentation;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.PersistentTypes.Atomicity;
using NTDLS.Katzebase.PersistentTypes.Schema;
using System.Diagnostics;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.PersistentTypes.Schema.PhysicalSchema;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to schemas.
    /// </summary>
    public class SchemaManager
    {
        private readonly EngineCore _core;
        private readonly string _rootCatalogFile;
        private PhysicalSchema? _rootPhysicalSchema = null;

        internal SchemaQueryHandlers QueryHandlers { get; private set; }

        public SchemaAPIHandlers APIHandlers { get; private set; }

        internal PhysicalSchema RootPhysicalSchema
        {
            get
            {
                try
                {
                    _rootPhysicalSchema ??= new PhysicalSchema()
                    {
                        Id = RootSchemaGUID,
                        DiskPath = _core.Settings.DataRootPath,
                        VirtualPath = string.Empty,
                        Name = string.Empty,
                        IsTemporary = false
                    };
                    return _rootPhysicalSchema;
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to obtain root schema.", ex);
                    throw;
                }
            }
        }

        internal SchemaManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new SchemaQueryHandlers(core);
                APIHandlers = new SchemaAPIHandlers(core);

                _rootCatalogFile = Path.Combine(core.Settings.DataRootPath, SchemaFile);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate SchemaManager.", ex);
                throw;
            }
        }

        /// <summary>
        /// To be executed after all other engine components have been initialized, for instance:
        /// we can't insert rows from the SchemaManager constructor because the document manager is not yet ready.
        /// </summary>
        public void PostInitialization()
        {
            //If the root schema doesn't exist, create a new empty one.
            if (File.Exists(_rootCatalogFile) == false)
            {
                LogManager.Information("Initializing root schema.");
                _core.IO.CreateSchemaArtifacts(RootPhysicalSchema);
            }

            bool doesMasterSchemaExist;
            using (var system = _core.Sessions.CreateEphemeralSystemSession())
            {
                doesMasterSchemaExist = _core.Schemas.AcquireVirtual(system.Transaction, "Master", LockOperation.Write, LockOperation.Write).Exists;
                system.Commit();
            }

            if (doesMasterSchemaExist == false)
            {
                LogManager.Information("Initializing master schema.");
                _core.Query.SystemExecuteAndCommitNonQuery("CreateMasterSchema.kbs");
                _core.Query.SystemExecuteAndCommitNonQuery("CreateDefaultUsersAndRoles.kbs");
                _core.Query.SystemExecuteAndCommitNonQuery("CreateSingleSchema.kbs");
                _core.Query.SystemExecuteAndCommitNonQuery("InitializeSingleSchema.kbs");
            }

            LogManager.Information("Initializing ephemeral schemas.");
            _core.Query.SystemExecuteAndCommitNonQuery("DropAndCreateTemporarySchema.kbs");
            _core.Query.SystemExecuteAndCommitNonQuery("InitializeTemporarySchema.kbs");
        }

        internal void Alter(Transaction transaction, string schemaName)
        {
            try
            {
                var physicalSchema = Acquire(transaction, schemaName, LockOperation.Write);
                var parentPhysicalSchema = AcquireParent(transaction, physicalSchema, LockOperation.Write);

                var parentRdb = _core.IO.AcquireRdb(parentPhysicalSchema.SchemaFilePath());

                var singleSchema = _core.IO.GetJson<PhysicalSchema>(transaction, parentRdb,
                    KbColumnFamilyName.Schema, new RdbKey(physicalSchema.Name), LockOperation.Write)
                    ?? throw new KbObjectNotFoundException($"Schema not found: [{physicalSchema.Name}].");

                //singleSchema.SomeOtherAttribute = 0;

                _core.IO.PutJson(transaction, parentRdb, KbColumnFamilyName.Schema, new RdbKey(physicalSchema.Name), singleSchema);

                if (physicalSchema.IsTemporary)
                {
                    if (transaction.IsUserCreated)
                    {
                        //If this is a long standing transaction, then we can keep track of these temp schemas and delete them automatically.
                        transaction.TemporarySchemas.Write((obj) => obj.Add(physicalSchema.VirtualPath));
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal void CreateSingleSchema(Transaction transaction, string schemaName)
        {
            try
            {
                //Lock the given schema, but also go ahead and place the same lock the parent schema to avoid deadlocks.
                var physicalSchema = AcquireVirtual(transaction, schemaName, LockOperation.Write, LockOperation.Write);
                if (physicalSchema.Exists)
                {
                    return; //The schema already exists, not much else to do.
                }

                var parentPhysicalSchema = AcquireParent(transaction, physicalSchema, LockOperation.Write);
                var parentRdb = _core.IO.AcquireRdb(parentPhysicalSchema.SchemaFilePath());

                if (!_core.IO.DoesKeyExist(transaction, parentRdb, KbColumnFamilyName.Schema, new RdbKey(physicalSchema.Name), LockOperation.Write))
                {
                    physicalSchema.Id = Guid.NewGuid();
                    physicalSchema.Name = physicalSchema.Name;
                    //physicalSchema.SomeOtherAttribute = 0;

                    _core.IO.CreateSchemaArtifacts(physicalSchema);

                    _core.IO.PutJson(transaction, parentRdb, KbColumnFamilyName.Schema, new RdbKey(physicalSchema.Name), physicalSchema);

                    if (physicalSchema.IsTemporary)
                    {
                        if (transaction.IsUserCreated)
                        {
                            //If this is a long standing transaction, then we can keep track of these temp schemas and delete them automatically.
                            transaction.TemporarySchemas.Write((obj) => obj.Add(physicalSchema.VirtualPath));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal void Drop(Transaction transaction, string schemaName)
        {
            try
            {
                var physicalSchema = AcquireVirtual(transaction, schemaName, LockOperation.Write, LockOperation.Write);
                if (physicalSchema.Exists == false)
                {
                    return; //The schema does not exists, not much else to do.
                }

                if (transaction.IsUserCreated)
                {
                    //NOTE: Dropping a schema is not a reversible action.
                    throw new Exception("Schemas cannot be dropped within a user transaction.");
                }

                var rdb = _core.IO.AcquireRdb(physicalSchema.ParentPhysicalSchema.SchemaFilePath());

                _core.IO.DeleteKey(transaction, rdb, KbColumnFamilyName.Schema, new RdbKey(physicalSchema.Name));
                _core.IO.CloseRdbsUnderPath(physicalSchema.DiskPath);
                if (Directory.Exists(physicalSchema.DiskPath))
                {
                    Directory.Delete(physicalSchema.DiskPath, true);
                }

                transaction.LockPathRecursive(LockOperation.Delete, new CacheKey(physicalSchema.DiskPath));

                var cacheKey = CacheManager.MakeCacheKey(physicalSchema.ParentPhysicalSchema.SchemaFilePath(), KbColumnFamilyName.Schema, physicalSchema.Name);
                _core.Cache.Remove(cacheKey);

                // Eject everything under the dropped schema's directory — documents, indexes,
                // sub-schema catalogs, and all nested sub-schema contents.
                transaction.Instrumentation.Measure(PerformanceCounter.CacheWrite, () =>
                    _core.Cache.RemoveItemsForPath(new CacheKey(physicalSchema.DiskPath)));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal List<PhysicalSchema> AcquireChildren(Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockOp)
        {
            try
            {
                var schemas = new List<PhysicalSchema>();
                var rdb = _core.IO.AcquireRdb(physicalSchema.SchemaFilePath());

                if (_core.IO.DoesKeyExist(transaction, rdb, KbColumnFamilyName.Schema, new RdbKey(physicalSchema.Name), lockOp, out _))
                {
                    var schemaCatalog = _core.IO.GetJsonList<PhysicalSchema>(transaction, rdb, KbColumnFamilyName.Schema, LockOperation.Read);

                    foreach (var catalogItem in schemaCatalog)
                    {
                        schemas.Add(new PhysicalSchema()
                        {
                            DiskPath = physicalSchema.DiskPath + "\\" + catalogItem.Name,
                            Id = catalogItem.Id,
                            Name = catalogItem.Name,
                            VirtualPath = physicalSchema.VirtualPath + ":" + catalogItem.Name
                        });
                    }
                }

                return schemas;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal PhysicalSchema AcquireParent(Transaction transaction, PhysicalSchema child, LockOperation lockOp)
        {
            try
            {
                if (child == RootPhysicalSchema)
                {
                    throw new KbGenericException("Root schema does not have a parent.");
                }

                if (child.VirtualPath == null)
                {
                    throw new KbNullException($"Value should not be null: [{nameof(child.VirtualPath)}].");
                }

                var segments = child.VirtualPath.Split(':');
                string parentSchema = string.Join(":", segments.Take(segments.Length - 1));
                return Acquire(transaction, parentSchema, lockOp);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Opens a schema for a desired access. Takes a virtual schema path 
        ///     (schema0:schema2:schema3) and converts to to a physical location.
        /// </summary>
        internal PhysicalSchema Acquire(Transaction transaction, string schemaName, LockOperation lockOp)
        {
            InstrumentationDurationToken? ptLockSchema = null;

            try
            {
                bool isTemporary = false;
                if (schemaName.StartsWith('#'))
                {
                    var session = _core.Sessions.ByProcessId(transaction.ProcessId);
                    schemaName = $"Temporary:{schemaName.Substring(1).Replace(':', '_')}_{session.ConnectionId}";
                    isTemporary = true;
                }

                ptLockSchema = transaction.Instrumentation.CreateToken<PhysicalSchema>(PerformanceCounter.Lock);
                schemaName = schemaName.Trim([':']).Trim();

                if (schemaName == string.Empty)
                {
                    return RootPhysicalSchema;
                }
                else
                {
                    var segments = schemaName.Split(':');
                    var thisSchemaName = segments[^1];

                    var schemaDiskPath = Path.Combine(_core.Settings.DataRootPath, string.Join("\\", segments));
                    var parentSchemaDiskPath = Directory.GetParent(schemaDiskPath)?.FullName;

                    var parentCatalogDiskPath = Path.Combine(parentSchemaDiskPath.EnsureNotNull(), SchemaFile);
                    var rdb = _core.IO.AcquireRdb(parentCatalogDiskPath);
                    if (_core.IO.DoesKeyExist(transaction, rdb, KbColumnFamilyName.Schema, new RdbKey(thisSchemaName), LockOperation.Stability) == false)
                    {
                        throw new KbObjectNotFoundException($"Schema not found: [{schemaName}].");
                    }

                    var physicalSchema = _core.IO.GetJson<PhysicalSchema>(transaction, rdb, KbColumnFamilyName.Schema, new RdbKey(thisSchemaName), LockOperation.Stability, out var _);

                    if (physicalSchema != null)
                    {
                        physicalSchema.Name = thisSchemaName;
                        physicalSchema.DiskPath = schemaDiskPath;
                        physicalSchema.VirtualPath = schemaName;
                        physicalSchema.IsTemporary = isTemporary;
                    }
                    else
                    {
                        throw new KbObjectNotFoundException($"Schema not found: [{schemaName}].");
                    }

                    transaction.LockPath(lockOp, new CacheKey(physicalSchema.DiskPath));

                    return physicalSchema;
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
            finally
            {
                ptLockSchema?.StopAndAccumulate();
            }
        }

        /// <summary>
        /// Opens a schema for a desired access even if it does not exist. Takes a virtual 
        ///     schema path (schema:schema2:schema3) and converts to to a physical location.
        /// </summary>
        /// <param name="transaction">Current transaction.</param>
        /// <param name="schemaName">Schema name to a acquire a lock on.</param>
        /// <param name="lockOp">Intended operation on the schema.</param>
        /// <param name="parentLockOp">Intended operation on the parent schema.</param>
        /// <returns></returns>
        internal VirtualSchema AcquireVirtual(Transaction transaction, string schemaName,
            LockOperation lockOp, LockOperation parentLockOp)
        {
            InstrumentationDurationToken? ptLockSchema = null;

            try
            {
                bool isTemporary = false;
                if (schemaName.StartsWith('#'))
                {
                    var session = _core.Sessions.ByProcessId(transaction.ProcessId);
                    schemaName = $"Temporary:{schemaName.Substring(1).Replace(':', '_')}_{session.ConnectionId}";
                    isTemporary = true;
                }

                ptLockSchema = transaction.Instrumentation.CreateToken<PhysicalSchema>(PerformanceCounter.Lock);
                schemaName = schemaName.Trim([':']).Trim();
                if (string.IsNullOrEmpty(schemaName))
                {
                    throw new KbEngineException("Cannot acquire virtual lock of root schema.");
                }

                var schemaSegments = schemaName.Split(':');
                var thisSchema = schemaSegments[^1];
                var parentSchema = string.Join(':', schemaSegments.Take(schemaSegments.Length - 1));

                var parentPhysicalSchema = Acquire(transaction, parentSchema, parentLockOp);

                var parentCatalogDiskPath = parentPhysicalSchema.SchemaFilePath();
                var rdb = _core.IO.AcquireRdb(parentCatalogDiskPath);

                var virtualSchema = _core.IO.GetJson<PhysicalSchema>(transaction, rdb, KbColumnFamilyName.Schema, new RdbKey(thisSchema), parentLockOp)
                    ?.ToVirtual(parentPhysicalSchema);
                if (virtualSchema != null)
                {
                    virtualSchema.Name = thisSchema;
                    virtualSchema.DiskPath = Path.Combine(_core.Settings.DataRootPath, string.Join("\\", schemaSegments));
                    virtualSchema.VirtualPath = schemaName;
                    virtualSchema.Exists = true;
                    virtualSchema.IsTemporary = isTemporary;
                }
                else
                {
                    virtualSchema = new VirtualSchema(parentPhysicalSchema)
                    {
                        Name = thisSchema,
                        DiskPath = _core.Settings.DataRootPath + "\\" + schemaName.Replace(':', '\\'),
                        VirtualPath = schemaName,
                        Exists = false,
                        IsTemporary = isTemporary

                    };
                }

                transaction.LockPath(lockOp, new CacheKey(virtualSchema.DiskPath));

                return virtualSchema;

            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
            finally
            {
                ptLockSchema?.StopAndAccumulate();
            }
        }

        internal PhysicalSchema? GetChild(Transaction transaction, PhysicalSchema physicalSchema, string schemaName, LockOperation lockop)
        {
            try
            {
                var rdb = _core.IO.AcquireRdb(physicalSchema.SchemaFilePath());

                var schemaCatalog = _core.IO.GetJsonList<PhysicalSchema>(transaction, rdb, KbColumnFamilyName.Schema, lockop);

                return schemaCatalog.FirstOrDefault(s => s.Name.Equals(schemaName, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal List<Tuple<string, string>> GetListOfChildren(Transaction transaction, string schemaName, int rowLimit)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
                var rdb = _core.IO.AcquireRdb(physicalSchema.SchemaFilePath());

                var schemaCatalog = _core.IO.GetJsonList<PhysicalSchema>(
                    transaction, rdb, KbColumnFamilyName.Schema, LockOperation.Read);

                var result = new List<Tuple<string, string>>();

                foreach (var item in schemaCatalog)
                {
                    if (rowLimit > 0 && result.Count >= rowLimit)
                    {
                        break;
                    }

                    result.Add(new Tuple<string, string>(item.Name, $"{physicalSchema.VirtualPath}:{item.Name}"));
                }

                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResult AnalyzePages(Transaction transaction, string schemaName, bool includeIndexDetails)
        {
            try
            {
                var physicalSchema = Acquire(transaction, schemaName, LockOperation.Read);

                var result = new KbQueryResult();
                result.AddField("Property");
                result.AddField("Value");

                // Scan the Documents column family for size statistics.
                // Use the raw iterator value bytes (protobuf) so we never need to deserialize.
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                var documentsCF = rdb.GetColumnFamily(KbColumnFamilyName.Documents);

                long docCount = 0;
                long totalBytes = 0;
                int minBytes = int.MaxValue;
                int maxBytes = 0;

                using (var iter = rdb.NewIterator(documentsCF))
                {
                    for (iter.SeekToFirst(); iter.Valid(); iter.Next())
                    {
                        transaction.EnsureActive();
                        int len = iter.Value().Length;
                        docCount++;
                        totalBytes += len;
                        if (len < minBytes) minBytes = len;
                        if (len > maxBytes) maxBytes = len;
                    }
                }

                if (docCount == 0)
                    minBytes = 0;

                double avgBytes = docCount > 0 ? (double)totalBytes / docCount : 0.0;

                var indexes = _core.Indexes.AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                result.AddRow(["Schema", physicalSchema.Name]);
                result.AddRow(["Document Count", $"{docCount:N0}"]);
                result.AddRow(["Total Data", $"{totalBytes / 1024.0:N2} KB"]);
                result.AddRow(["Min Doc Size", $"{minBytes:N0} B"]);
                result.AddRow(["Max Doc Size", $"{maxBytes:N0} B"]);
                result.AddRow(["Avg Doc Size", $"{avgBytes:N2} B"]);
                result.AddRow(["Index Count", $"{indexes.Count:N0}"]);

                if (includeIndexDetails)
                {
                    foreach (var physicalIndex in indexes)
                    {
                        transaction.EnsureActive();

                        var idxCF = rdb.GetColumnFamily(new RdbKey(physicalIndex.Id));

                        long distinctKeys = 0;
                        long totalDocRefs = 0;
                        int minDocs = int.MaxValue;
                        int maxDocs = 0;

                        using var idxIter = rdb.NewIterator(idxCF);
                        for (idxIter.SeekToFirst(); idxIter.Valid(); idxIter.Next())
                        {
                            int docCount2 = idxIter.Value().Length / sizeof(uint);
                            distinctKeys++;
                            totalDocRefs += docCount2;
                            if (docCount2 < minDocs) minDocs = docCount2;
                            if (docCount2 > maxDocs) maxDocs = docCount2;
                        }

                        if (distinctKeys == 0)
                            minDocs = 0;

                        double avgDocs = distinctKeys > 0 ? (double)totalDocRefs / distinctKeys : 0.0;
                        double selectivity = totalDocRefs > 0 ? (double)distinctKeys / totalDocRefs * 100.0 : 100.0;
                        string attrs = string.Join(", ", physicalIndex.Attributes.Select(a => a.Field));

                        result.AddRow([$"Index: {physicalIndex.Name}", $"[{attrs}]"]);
                        result.AddRow([$"  Unique", $"{physicalIndex.IsUnique}"]);
                        result.AddRow([$"  Distinct Keys", $"{distinctKeys:N0}"]);
                        result.AddRow([$"  Total Doc Refs", $"{totalDocRefs:N0}"]);
                        result.AddRow([$"  Min/Max/Avg Docs per Key", $"{minDocs:N0} / {maxDocs:N0} / {avgDocs:N2}"]);
                        result.AddRow([$"  Selectivity", $"{selectivity:N2}%"]);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }
    }
}
