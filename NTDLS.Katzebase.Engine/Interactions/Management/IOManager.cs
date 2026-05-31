using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.Engine.Locking;
using NTDLS.Katzebase.PersistentTypes.Schema;
using RocksDbSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Internal core class methods for locking, reading, writing and managing tasks related to disk I/O.
    /// </summary>
    internal class IOManager
    {
        private readonly EngineCore _core;
        internal IOManager(EngineCore core)
        {
            _core = core;
        }

        internal static void PutJsonNonTrackedPretty(string filePath, object deserializedObject)
        {
            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(deserializedObject, Formatting.Indented));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{filePath}].", ex);
                throw;
            }
        }

        #region Getters.

        internal static T GetJsonNonTracked<T>(string filePath)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath)).EnsureNotNull();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{filePath}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Reads from a RDB with transactional tracking, locking and deferred IO, and without caching.
        /// </summary>
        public T? GetNotTracked<T>(Rdb rdb, KbColumnFamilyName columnFamilyName, byte[] key, IOFormat format)
        {
            try
            {
                T? deserializedObject;

                if (format == IOFormat.JSON)
                {
                    var bytes = rdb.Get(key, columnFamilyName);
                    if (bytes == null)
                    {
                        return default;
                    }

                    deserializedObject = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));

                }
                else if (format == IOFormat.PBuf)
                {
                    var bytes = rdb.Get(key, columnFamilyName);
                    if (bytes == null)
                    {
                        return default;
                    }

                    using var input = new MemoryStream(bytes);
                    deserializedObject = ProtoBuf.Serializer.Deserialize<T>(input);
                }
                else
                {
                    throw new NotImplementedException($"IO format is not implemented: [{format}].");
                }

                return deserializedObject.EnsureNotNull();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdb.Path}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Reads from a RDB with transactional tracking, locking and deferred IO, and without caching.
        /// </summary>
        public byte[] GetNotTrackedRaw(Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key)
        {
            try
            {
                return rdb.Get(key.Bytes, columnFamilyName);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdb.Path}].", ex);
                throw;
            }
        }

        internal T? GetJson<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, LockOperation lockOperation, out ObjectLockKey? acquiredLockKey, bool populateCache = true)
            => InternalTrackedGet<T>(transaction, rdb, columnFamilyName, key, lockOperation, IOFormat.JSON, out acquiredLockKey, populateCache);

        internal T? GetPBuf<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, LockOperation lockOperation, out ObjectLockKey? acquiredLockKey, bool populateCache = true)
            => InternalTrackedGet<T>(transaction, rdb, columnFamilyName, key, lockOperation, IOFormat.PBuf, out acquiredLockKey, populateCache);

        internal T? GetJson<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, LockOperation lockOperation, bool populateCache = true)
            => InternalTrackedGet<T>(transaction, rdb, columnFamilyName, key, lockOperation, IOFormat.JSON, out _, populateCache);

        internal T? GetPBuf<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, LockOperation lockOperation, bool populateCache = true)
            => InternalTrackedGet<T>(transaction, rdb, columnFamilyName, key, lockOperation, IOFormat.PBuf, out _, populateCache);

        /// <summary>
        /// Reads from a RDB with transactional tracking, locking and deferred IO, and without caching.
        /// </summary>
        public T? InternalTrackedGet<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key,
            LockOperation lockOperation, IOFormat format, out ObjectLockKey? acquiredLockKey, bool populateCache = true)
        {
            try
            {
                transaction.EnsureActive();
                var cacheKey = CacheManager.MakeCacheKey(rdb.Path, columnFamilyName, key);
                acquiredLockKey = transaction.LockSingleObject(lockOperation, cacheKey);
                transaction.RecordKeyRead(rdb.Path, columnFamilyName, key, cacheKey);

                if (_core.Settings.DeferredIOEnabled)
                {
                    var result = transaction.DeferredIOs.ReadNullable((dio) =>
                    {
                        var ptDeferredWriteRead = transaction.Instrumentation.CreateToken(PerformanceCounter.DeferredRead);
                        bool wasDeferred = dio.GetDeferredDiskIO<T>(cacheKey, out var deferredReference);
                        ptDeferredWriteRead?.StopAndAccumulate();

                        if (wasDeferred)
                        {
                            _core.Health.IncrementDiscrete(HealthCounterType.IODeferredReads);
                            LogManager.Trace($"IO:CacheHit:{transaction.ProcessId}->{cacheKey}");
                            return deferredReference;
                        }
                        return default;
                    });

                    if (result != null)
                    {
                        return result;
                    }
                }

                if (_core.Settings.CacheEnabled)
                {
                    var ptCacheRead = transaction.Instrumentation.CreateToken<T>(PerformanceCounter.CacheRead);
                    bool cacheHit = _core.Cache.TryGet(cacheKey, out var cachedObject);
                    ptCacheRead?.StopAndAccumulate();

                    if (cacheHit && cachedObject != null)
                    {
                        _core.Health.IncrementDiscrete(HealthCounterType.IOCacheReadHits);
                        LogManager.Trace($"IO:CacheHit:{transaction.ProcessId}->{cacheKey}");

                        return (T)cachedObject;
                    }
                }

                _core.Health.IncrementDiscrete(HealthCounterType.IOCacheReadMisses);
                LogManager.Trace($"IO:Read:{transaction.ProcessId}->{cacheKey}");

                T? deserializedObject;
                int estimatedObjectSize = 0;

                if (format == IOFormat.JSON)
                {
                    var bytes = transaction.Instrumentation.Measure(PerformanceCounter.IORead, () => rdb.Get(key.Bytes, columnFamilyName));
                    if (bytes == null)
                    {
                        return default;
                    }

                    estimatedObjectSize = bytes.Length;

                    deserializedObject = transaction.Instrumentation.Measure(PerformanceCounter.Deserialize, () =>
                        JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes)));
                }
                else if (format == IOFormat.PBuf)
                {
                    var bytes = transaction.Instrumentation.Measure(PerformanceCounter.IORead, () => rdb.Get(key.Bytes, columnFamilyName));
                    if (bytes == null)
                    {
                        return default;
                    }

                    estimatedObjectSize = bytes.Length;

                    deserializedObject = transaction.Instrumentation.Measure(PerformanceCounter.Deserialize, () =>
                    {
                        using var input = new MemoryStream(bytes);
                        return ProtoBuf.Serializer.Deserialize<T>(input);
                    });
                }
                else
                {
                    throw new NotImplementedException($"IO format is not implemented: [{format}].");
                }

                if (_core.Settings.CacheEnabled && deserializedObject != null && populateCache)
                {
                    transaction.Instrumentation.Measure(PerformanceCounter.CacheWrite, () =>
                        _core.Cache.Set(cacheKey, deserializedObject, estimatedObjectSize));

                    _core.Health.IncrementDiscrete(HealthCounterType.IOCacheReadAdditions);
                }

                return deserializedObject.EnsureNotNull();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{rdb.Path}].", ex);
                throw;
            }
        }

        internal List<T> GetJsonList<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, LockOperation lockOperation, out ObjectLockKey? acquiredLockKey)
            => InternalTrackedGetList<T>(transaction, rdb, columnFamilyName, lockOperation, IOFormat.JSON, out acquiredLockKey);

        internal List<T> GetPBufList<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, LockOperation lockOperation, out ObjectLockKey? acquiredLockKey)
            => InternalTrackedGetList<T>(transaction, rdb, columnFamilyName, lockOperation, IOFormat.PBuf, out acquiredLockKey);

        internal List<T> GetJsonList<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, LockOperation lockOperation)
            => InternalTrackedGetList<T>(transaction, rdb, columnFamilyName, lockOperation, IOFormat.JSON, out _);

        internal List<T> GetPBufList<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, LockOperation lockOperation)
            => InternalTrackedGetList<T>(transaction, rdb, columnFamilyName, lockOperation, IOFormat.PBuf, out _);

        /// <summary>
        /// Reads from a RDB with transactional tracking, locking and deferred IO, and without caching.
        /// </summary>
        public List<T> InternalTrackedGetList<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName,
            LockOperation lockOperation, IOFormat format, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                transaction.EnsureActive();
                acquiredLockKey = transaction.LockSingleObject(lockOperation, CacheManager.MakeCacheKey(rdb.Path, columnFamilyName));

                /*
                //We cant cache lists because we have no way to invalidate them on mutation of any single item in the list.
                if (_core.Settings.CacheEnabled)
                {
                    var ptCacheRead = transaction.Instrumentation.CreateToken<T>(PerformanceCounter.CacheRead);
                    bool cacheHit = _core.Cache.TryGet(listCacheKey, out var cachedObject);
                    ptCacheRead?.StopAndAccumulate();

                    if (cacheHit && cachedObject != null)
                    {
                        _core.Health.IncrementDiscrete(HealthCounterType.IOCacheReadHits);
                        LogManager.Trace($"IO:CacheHit:{transaction.ProcessId}->{listCacheKey}");

                        return (List<T>)cachedObject;
                    }
                }
                */

                _core.Health.IncrementDiscrete(HealthCounterType.IOCacheReadMisses);

                var deserializedObject = new List<T>();

                if (format == IOFormat.JSON)
                {
                    using var iterator = rdb.NewIterator(columnFamilyName);
                    for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
                    {
                        var cacheKey = CacheManager.MakeCacheKey(rdb.Path, columnFamilyName, new RdbKey(iterator.Key()));

                        transaction.RecordKeyRead(rdb.Path, columnFamilyName, new RdbKey(iterator.Key()), cacheKey);

                        if (_core.Settings.DeferredIOEnabled)
                        {
                            var result = transaction.DeferredIOs.ReadNullable((dio) =>
                            {
                                var ptDeferredWriteRead = transaction.Instrumentation.CreateToken(PerformanceCounter.DeferredRead);
                                bool wasDeferred = dio.GetDeferredDiskIO<T>(cacheKey, out var deferredReference);
                                ptDeferredWriteRead?.StopAndAccumulate();

                                if (wasDeferred)
                                {
                                    _core.Health.IncrementDiscrete(HealthCounterType.IODeferredReads);
                                    LogManager.Trace($"IO:CacheHit:{transaction.ProcessId}->{cacheKey}");
                                    return deferredReference;
                                }
                                return default;
                            });

                            if (result != null)
                            {
                                deserializedObject.Add(result);
                            }
                        }
                        else
                        {
                            var obj = transaction.Instrumentation.Measure(PerformanceCounter.Deserialize, () =>
                            JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(iterator.Value())))
                            ?? throw new Exception($"JSON deserialization resulted in null for file: [{rdb.Path}].");

                            deserializedObject.Add(obj);
                        }
                    }
                }
                else if (format == IOFormat.PBuf)
                {
                    using var iterator = rdb.NewIterator(columnFamilyName);
                    for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
                    {
                        var cacheKey = CacheManager.MakeCacheKey(rdb.Path, columnFamilyName, new RdbKey(iterator.Key()));

                        transaction.RecordKeyRead(rdb.Path, columnFamilyName, new RdbKey(iterator.Key()), cacheKey);

                        if (_core.Settings.DeferredIOEnabled)
                        {
                            var result = transaction.DeferredIOs.ReadNullable((dio) =>
                            {
                                var ptDeferredWriteRead = transaction.Instrumentation.CreateToken(PerformanceCounter.DeferredRead);
                                bool wasDeferred = dio.GetDeferredDiskIO<T>(cacheKey, out var deferredReference);
                                ptDeferredWriteRead?.StopAndAccumulate();

                                if (wasDeferred)
                                {
                                    _core.Health.IncrementDiscrete(HealthCounterType.IODeferredReads);
                                    LogManager.Trace($"IO:CacheHit:{transaction.ProcessId}->{cacheKey}");
                                    return deferredReference;
                                }
                                return default;
                            });

                            if (result != null)
                            {
                                deserializedObject.Add(result);
                            }
                        }
                        else
                        {
                            var obj = transaction.Instrumentation.Measure(PerformanceCounter.Deserialize, () =>
                            {
                                using var input = new MemoryStream(iterator.Value());
                                return ProtoBuf.Serializer.Deserialize<T>(input);
                            }) ?? throw new Exception($"PBuf deserialization resulted in null for file: [{rdb.Path}].");
                            deserializedObject.Add(obj);
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException($"IO format is not implemented: [{format}].");
                }

                return deserializedObject.EnsureNotNull();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{rdb.Path}].", ex);
                throw;
            }
        }

        #endregion

        #region RocksDb Helpers.

        // Lazy<T> wrapper prevents ConcurrentDictionary.GetOrAdd from opening the same RocksDB
        // instance twice. GetOrAdd does not guarantee the factory runs only once — multiple threads
        // can race through and each call RocksDb.Open, with the second failing to acquire the LOCK
        // file. Storing a Lazy<RocksDb> means the factory (the cheap part) can run multiple times,
        // but only one Lazy wins the slot. RocksDb.Open is then called exactly once when .Value
        // is first accessed, guarded by Lazy's default ExecutionAndPublication thread-safety mode.
        private readonly ConcurrentDictionary<string, Lazy<Rdb>> _rdbInstances =
            new(StringComparer.InvariantCultureIgnoreCase);

        internal void CloseRdb(string rdbPath)
        {
            if (_rdbInstances.TryRemove(rdbPath, out var lazy) && lazy.IsValueCreated)
                lazy.Value.Dispose();
        }

        /// <summary>
        /// Closes and disposes every open RocksDB instance whose file path lives inside
        /// the given directory (inclusive). Used before recursively deleting a schema tree
        /// so that every child schema's RDB files are released before the directory is removed.
        /// </summary>
        internal void CloseRdbsUnderPath(string directoryPath)
        {
            // Normalize to a prefix that every file path under this directory will start with.
            var prefix = directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                         + Path.DirectorySeparatorChar;

            foreach (var key in _rdbInstances.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
                    || key.Equals(directoryPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (_rdbInstances.TryRemove(key, out var lazy) && lazy.IsValueCreated)
                        lazy.Value.Dispose();
                }
            }
        }

        internal Rdb AcquireDocumentsRdb(PhysicalSchema physicalSchema)
        {
            var documentsFilePath = physicalSchema.DocumentsFilePath();

            var lazy = _rdbInstances.GetOrAdd(documentsFilePath, path =>
            {
                var options = new DbOptions().SetCreateIfMissing(true).SetCreateMissingColumnFamilies(true);

                var defaultCfOptions = new ColumnFamilyOptions()
                    .SetBlockBasedTableFactory(new BlockBasedTableOptions().SetNoBlockCache(true))
                    .SetWalTtlSeconds(0);

                var documentsCfOptions = new ColumnFamilyOptions()
                    .SetBlockBasedTableFactory(new BlockBasedTableOptions().SetNoBlockCache(true))
                    .SetWalTtlSeconds(0);

                var columnFamilies = new ColumnFamilies();
                foreach (var cf in RocksDb.ListColumnFamilies(options, documentsFilePath))
                {
                    if (cf.Equals(KbColumnFamilyName.Documents.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        columnFamilies.Add(cf, documentsCfOptions);
                    }
                    else
                    {
                        columnFamilies.Add(cf, defaultCfOptions);
                    }
                }

                var instance = RocksDb.Open(options, documentsFilePath, columnFamilies);

                return new Lazy<Rdb>(() => new Rdb(path, instance));
            });

            try
            {
                return lazy.Value;
            }
            catch
            {
                // Lazy<T> caches exceptions — remove the faulted entry so the caller's
                // recovery path (e.g. CreateDocumentsRdb) can retry with a fresh instance.
                _rdbInstances.TryRemove(documentsFilePath, out _);
                throw;
            }
        }

        internal void CreateSchemaArtifacts(PhysicalSchema physicalSchema)
        {
            try
            {
                Directory.CreateDirectory(physicalSchema.DiskPath);

                var rdbOptions = new DbOptions().SetCreateIfMissing(true).SetCreateMissingColumnFamilies(true);

                var defaultCfOptions = new ColumnFamilyOptions()
                    .SetBlockBasedTableFactory(new BlockBasedTableOptions().SetNoBlockCache(true))
                    .SetWalTtlSeconds(0);

                //Create schema catalog RDB with necessary column families.
                var schemaCFs = new ColumnFamilies
                {
                    { KbColumnFamilyName.Schema.ToString(), defaultCfOptions }, //Child schema definitions.
                    { KbColumnFamilyName.Procedures.ToString(), defaultCfOptions }, //Stored procedures.
                    { KbColumnFamilyName.Identity.ToString(), defaultCfOptions } //Identity management for auto-incrementing keys, etc.
                };
                using var schemaRdbInstance = RocksDb.Open(rdbOptions, physicalSchema.SchemaFilePath(), schemaCFs);
                schemaRdbInstance.Dispose();

                //Create documents RDB with necessary column families.

                var documentsCfOptions = new ColumnFamilyOptions()
                    .SetBlockBasedTableFactory(new BlockBasedTableOptions().SetNoBlockCache(true))
                    .SetWalTtlSeconds(0);

                /*
                if (physicalSchema.someOption)
                {
                    documentsCfOptions.SomeOption(true);
                }
                */

                var documentsCFs = new ColumnFamilies
                {
                    { KbColumnFamilyName.Documents.ToString(), documentsCfOptions }, //Document data.
                    { KbColumnFamilyName.Identity.ToString(), defaultCfOptions },  //Identity management for auto-incrementing keys, etc.
                    { KbColumnFamilyName.Indexes.ToString(), defaultCfOptions },   //Indexes metadata for the documents.
                    { KbColumnFamilyName.Policy.ToString(), defaultCfOptions }     //Schema security policies.
                };
                using var rdbInstance = RocksDb.Open(rdbOptions, physicalSchema.DocumentsFilePath(), documentsCFs);
                rdbInstance.Dispose();

                //Create an initial identity value for the documents RDB.
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                _core.IO.PutNonTrackedRaw(rdb, KbColumnFamilyName.Identity, new RdbKey(PrimaryIdentityKey), BitConverter.GetBytes(1U));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()}.", ex);
                throw;
            }
        }

        internal Rdb AcquireRdb(string rdbPath)
        {
            var lazy = _rdbInstances.GetOrAdd(rdbPath, path =>
                new Lazy<Rdb>(() => new Rdb(path)));

            try
            {
                return lazy.Value;
            }
            catch
            {
                // Lazy<T> caches exceptions — remove the faulted entry so the caller's
                // recovery path (e.g. CreateDocumentsRdb) can retry with a fresh instance.
                _rdbInstances.TryRemove(rdbPath, out _);
                throw;
            }
        }

        internal bool DoesKeyExist(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, LockOperation intendedOperation)
            => DoesKeyExist(transaction, rdb, columnFamilyName, key, intendedOperation, out _);

        internal bool DoesKeyExist(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName,
            RdbKey key, LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey)
        {
            transaction.EnsureActive();
            var cacheKey = CacheManager.MakeCacheKey(rdb.Path, columnFamilyName, key);
            acquiredLockKey = transaction.LockSingleObject(intendedOperation, cacheKey);
            return rdb.Get(key.Bytes, columnFamilyName) != null;
        }

        internal void DeleteKey(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key)
        {
            transaction.EnsureActive();
            var cacheKey = CacheManager.MakeCacheKey(rdb.Path, columnFamilyName, key);
            transaction.LockSingleObject(LockOperation.Delete, cacheKey);

            transaction.RecordKeyDelete(rdb.Path, columnFamilyName, key, cacheKey, rdb.Get(key.Bytes, columnFamilyName));
            rdb.Remove(key.Bytes, columnFamilyName);
        }

        #endregion

        #region Putters.

        internal void PutNonTrackedButCached<T>(Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, T obj, IOFormat format)
            where T : notnull
        {
            try
            {
                var cacheKey = CacheManager.MakeCacheKey(rdb.Path, columnFamilyName, key);

                int approximateSizeInBytes = 0;

                if (format == IOFormat.JSON)
                {
                    string text = JsonConvert.SerializeObject(obj);

                    var bytes = Encoding.UTF8.GetBytes(text);

                    rdb.Put(key.Bytes, bytes, columnFamilyName);

                    approximateSizeInBytes = bytes.Length;
                }
                else if (format == IOFormat.PBuf)
                {
                    using var output = new MemoryStream();
                    ProtoBuf.Serializer.Serialize(output, obj);

                    var bytes = output.ToArray();
                    rdb.Put(key.Bytes, bytes, columnFamilyName);
                    approximateSizeInBytes = bytes.Length;
                }
                else
                {
                    throw new NotImplementedException($"IO format is not implemented: [{format}].");
                }

                if (_core.Settings.CacheEnabled)
                {
                    _core.Cache.Set(cacheKey, obj, approximateSizeInBytes);

                    _core.Health.IncrementDiscrete(HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdb.Path}].", ex);
                throw;
            }
        }

        internal void PutNonTrackedRaw(Rdb rdb, RdbKey columnFamily, RdbKey key, byte[] objBytes)
        {
            try
            {
                var cf = rdb.GetColumnFamily(columnFamily);
                rdb.Put(key.Bytes, objBytes, cf);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdb.Path}].", ex);
                throw;
            }
        }

        internal void PutNonTrackedRaw(Rdb rdb, RdbColumnFamily columnFamily, RdbKey key, byte[] objBytes)
        {
            try
            {
                rdb.Put(key.Bytes, objBytes, columnFamily);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdb.Path}].", ex);
                throw;
            }
        }

        internal void PutNonTrackedRaw(Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, byte[] objBytes)
        {
            try
            {
                rdb.Put(key.Bytes, objBytes, columnFamilyName);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdb.Path}].", ex);
                throw;
            }
        }

        internal void PutNonTracked<T>(Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, T obj, IOFormat format)
            where T : notnull
        {
            try
            {
                if (format == IOFormat.JSON)
                {
                    string text = JsonConvert.SerializeObject(obj);
                    var bytes = Encoding.UTF8.GetBytes(text);
                    rdb.Put(key.Bytes, bytes, columnFamilyName);
                }
                else if (format == IOFormat.PBuf)
                {
                    using var output = new MemoryStream();
                    ProtoBuf.Serializer.Serialize(output, obj);
                    rdb.Put(key.Bytes, output.ToArray(), columnFamilyName);
                }
                else
                {
                    throw new NotImplementedException($"IO format is not implemented: [{format}].");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdb.Path}].", ex);
                throw;
            }
        }

        internal void PutJson(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, object obj)
            => InternalTrackedPut(transaction, rdb, columnFamilyName, key, obj, LockOperation.Write, IOFormat.JSON);

        internal void PutPBuf(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, object obj)
            => InternalTrackedPut(transaction, rdb, columnFamilyName, key, obj, LockOperation.Write, IOFormat.PBuf);

        /// <summary>
        /// Writes to a RDB with transactional tracking, locking and deferred IO.
        /// </summary>
        public void InternalTrackedPut<T>(Transaction transaction, Rdb rdb, KbColumnFamilyName columnFamilyName, RdbKey key, T obj, LockOperation? lockOperation, IOFormat format)
            where T : notnull
        {
            try
            {
                transaction.EnsureActive();
                var cacheKey = CacheManager.MakeCacheKey(rdb.Path, columnFamilyName, key);

                transaction.LockSingleObject(LockOperation.Write, cacheKey);

                bool doesKeyExist = DoesKeyExist(transaction, rdb, columnFamilyName, key, LockOperation.Write, out _);
                if (doesKeyExist)
                {
                    transaction.RecordKeyAlter(rdb.Path, columnFamilyName, key, cacheKey, rdb.Get(key.Bytes, columnFamilyName));
                }
                else
                {
                    transaction.RecordKeyCreate(rdb.Path, columnFamilyName, key, cacheKey);
                }

                if (_core.Settings.DeferredIOEnabled)
                {
                    transaction.DeferredIOs.Write((dio) =>
                    {
                        transaction.Instrumentation.Measure(PerformanceCounter.DeferredWrite, () =>
                            dio.PutDeferredDiskIO(cacheKey, rdb.Path, columnFamilyName, obj, key, format));
                    });

                    _core.Health.IncrementDiscrete(HealthCounterType.IODeferredWrites);

                    //We can skip caching because we write this to the deferred IO cache - which
                    //  is infinitely more deterministic than the memory cache with auto-ejections.
                    return;
                }

                if (format == IOFormat.JSON)
                {
                    string text = transaction.Instrumentation.Measure(PerformanceCounter.Serialize, () =>
                        JsonConvert.SerializeObject(obj));

                    transaction.Instrumentation.Measure(PerformanceCounter.IOWrite, () =>
                        rdb.Put(key.Bytes, Encoding.UTF8.GetBytes(text), columnFamilyName));
                }
                else if (format == IOFormat.PBuf)
                {
                    var bytes = transaction.Instrumentation.Measure(PerformanceCounter.Serialize, () =>
                    {
                        using var output = new MemoryStream();
                        ProtoBuf.Serializer.Serialize(output, obj);
                        return output.ToArray();
                    });

                    transaction.Instrumentation.Measure(PerformanceCounter.IOWrite, () =>
                        rdb.Put(key.Bytes, bytes, columnFamilyName));
                }
                else
                {
                    throw new NotImplementedException($"IO format is not implemented: [{format}].");
                }

                // Write-through caching is intentionally omitted: reads populate cache on demand.
                // Caching every write inflates memory 3-5x (C# object vs. serialized bytes estimate)
                // and is counterproductive during bulk import where those entries are rarely re-read.
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{rdb.Path}].", ex);
                throw;
            }
        }

        #endregion
    }
}
