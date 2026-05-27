using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.Engine.Locking;
using NTDLS.Katzebase.Shared;
using RocksDbSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    //Internal core class methods for locking, reading, writing and managing tasks related to disk I/O.
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
        public T? GetNotTracked<T>(string rdbPath, KbColumnFamily columnFamilyName, byte[] key, IOFormat format)
        {
            try
            {
                var rdb = AcquireRdb(rdbPath);
                var columnFamily = GetColumnFamily(rdb, columnFamilyName);

                T? deserializedObject;

                if (format == IOFormat.JSON)
                {
                    var bytes = rdb.Get(key, columnFamily);
                    if (bytes == null)
                    {
                        return default;
                    }

                    deserializedObject = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));

                }
                else if (format == IOFormat.PBuf)
                {
                    var bytes = rdb.Get(key, columnFamily);
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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdbPath}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Reads from a RDB with transactional tracking, locking and deferred IO, and without caching.
        /// </summary>
        public byte[] GetNotTrackedRaw(string rdbPath, KbColumnFamily columnFamilyName, RdbKey key)
        {
            try
            {
                var rdb = AcquireRdb(rdbPath);
                var columnFamily = GetColumnFamily(rdb, columnFamilyName);

                return rdb.Get(key.Bytes, columnFamily);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdbPath}].", ex);
                throw;
            }
        }

        internal T? GetJson<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, LockOperation lockOperation, out ObjectLockKey? acquiredLockKey)
            => InternalTrackedGet<T>(transaction, rdbPath, columnFamilyName, key, lockOperation, IOFormat.JSON, out acquiredLockKey);

        internal T? GetPBuf<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, LockOperation lockOperation, out ObjectLockKey? acquiredLockKey)
            => InternalTrackedGet<T>(transaction, rdbPath, columnFamilyName, key, lockOperation, IOFormat.PBuf, out acquiredLockKey);

        internal T? GetJson<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, LockOperation lockOperation)
            => InternalTrackedGet<T>(transaction, rdbPath, columnFamilyName, key, lockOperation, IOFormat.JSON, out _);

        internal T? GetPBuf<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, LockOperation lockOperation)
            => InternalTrackedGet<T>(transaction, rdbPath, columnFamilyName, key, lockOperation, IOFormat.PBuf, out _);

        /// <summary>
        /// Reads from a RDB with transactional tracking, locking and deferred IO, and without caching.
        /// </summary>
        public T? InternalTrackedGet<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, RdbKey key,
            LockOperation lockOperation, IOFormat format, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                transaction.EnsureActive();
                acquiredLockKey = transaction.LockFile(lockOperation, rdbPath);
                var cacheKey = CacheManager.MakeCacheKey(rdbPath, columnFamilyName, key);
                transaction.RecordKeyRead(rdbPath, columnFamilyName, key, cacheKey);
                var rdb = AcquireRdb(rdbPath);
                var columnFamily = GetColumnFamily(rdb, columnFamilyName);

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
                            LogManager.Trace($"IO:CacheHit:{transaction.ProcessId}->{rdbPath}");
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
                int approximateSizeInBytes = 0;

                if (format == IOFormat.JSON)
                {
                    var bytes = transaction.Instrumentation.Measure(PerformanceCounter.IORead, () => rdb.Get(key.Bytes, columnFamily));
                    if (bytes == null)
                    {
                        return default;
                    }

                    deserializedObject = transaction.Instrumentation.Measure(PerformanceCounter.Deserialize, () =>
                        JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes)));

                }
                else if (format == IOFormat.PBuf)
                {
                    var bytes = transaction.Instrumentation.Measure(PerformanceCounter.IORead, () => rdb.Get(key.Bytes, columnFamily));
                    if (bytes == null)
                    {
                        return default;
                    }

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

                if (_core.Settings.CacheEnabled && deserializedObject != null)
                {
                    transaction.Instrumentation.Measure(PerformanceCounter.CacheWrite, () =>
                        _core.Cache.Set(cacheKey, deserializedObject, approximateSizeInBytes));

                    _core.Health.IncrementDiscrete(HealthCounterType.IOCacheReadAdditions);
                }

                return deserializedObject.EnsureNotNull();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{rdbPath}].", ex);
                throw;
            }
        }

        internal List<T> GetJsonList<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, LockOperation lockOperation, out ObjectLockKey? acquiredLockKey)
            => InternalTrackedGetList<T>(transaction, rdbPath, columnFamilyName, lockOperation, IOFormat.JSON, out acquiredLockKey);

        internal List<T> GetPBufList<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, LockOperation lockOperation, out ObjectLockKey? acquiredLockKey)
            => InternalTrackedGetList<T>(transaction, rdbPath, columnFamilyName, lockOperation, IOFormat.PBuf, out acquiredLockKey);

        internal List<T> GetJsonList<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, LockOperation lockOperation)
            => InternalTrackedGetList<T>(transaction, rdbPath, columnFamilyName, lockOperation, IOFormat.JSON, out _);

        internal List<T> GetPBufList<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, LockOperation lockOperation)
            => InternalTrackedGetList<T>(transaction, rdbPath, columnFamilyName, lockOperation, IOFormat.PBuf, out _);

        /// <summary>
        /// Reads from a RDB with transactional tracking, locking and deferred IO, and without caching.
        /// </summary>
        public List<T> InternalTrackedGetList<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName,
            LockOperation lockOperation, IOFormat format, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                transaction.EnsureActive();
                acquiredLockKey = transaction.LockFile(lockOperation, rdbPath);
                var rdb = AcquireRdb(rdbPath);
                var columnFamily = GetColumnFamily(rdb, columnFamilyName);

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
                    using var iterator = rdb.NewIterator(columnFamily);
                    for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
                    {
                        var cacheKey = CacheManager.MakeCacheKey(rdbPath, columnFamilyName, new RdbKey(iterator.Key()));

                        transaction.RecordKeyRead(rdbPath, columnFamilyName, new RdbKey(iterator.Key()), cacheKey);

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
                            ?? throw new Exception($"JSON deserialization resulted in null for file: [{rdbPath}].");

                            deserializedObject.Add(obj);
                        }
                    }
                }
                else if (format == IOFormat.PBuf)
                {
                    using var iterator = rdb.NewIterator(columnFamily);
                    for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
                    {
                        var cacheKey = CacheManager.MakeCacheKey(rdbPath, columnFamilyName, new RdbKey(iterator.Key()));

                        transaction.RecordKeyRead(rdbPath, columnFamilyName, new RdbKey(iterator.Key()), cacheKey);

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
                            }) ?? throw new Exception($"PBuf deserialization resulted in null for file: [{rdbPath}].");
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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{rdbPath}].", ex);
                throw;
            }
        }

        #endregion

        #region RocksDb Helpers.

        private readonly ConcurrentDictionary<string, RocksDb> _rdbInstances =
            new(StringComparer.InvariantCultureIgnoreCase);

        public ColumnFamilyHandle GetColumnFamily(RocksDb rdb, KbColumnFamily name)
            => GetColumnFamily(rdb, name.ToString());

        public ColumnFamilyHandle GetColumnFamily(RocksDb rdb, RdbKey key)
            => GetColumnFamily(rdb, key.ToString());

        public ColumnFamilyHandle GetColumnFamily(RocksDb rdb, string name)
            => KbLocalCache.GetOrCreateSliding10Minutes($"{rdb.Path}:ColumnFamily:{name}", () =>
            {
                try { return rdb.GetColumnFamily(name); }
                catch { return rdb.CreateColumnFamily(new ColumnFamilyOptions(), name); }
            }).EnsureNotNull();

        public ColumnFamilyHandle CreateColumnFamily(RocksDb rdb, KbColumnFamily name)
            => CreateColumnFamily(rdb, name.ToString());

        public ColumnFamilyHandle CreateColumnFamily(RocksDb rdb, RdbKey key)
            => CreateColumnFamily(rdb, key.ToString());

        public ColumnFamilyHandle CreateColumnFamily(RocksDb rdb, string name)
            => KbLocalCache.GetOrCreateSliding10Minutes($"{rdb.Path}:ColumnFamily:{name}", () => rdb.CreateColumnFamily(new ColumnFamilyOptions(), name)).EnsureNotNull();

        public void DropColumnFamily(RocksDb rdb, KbColumnFamily name)
        {
            rdb.DropColumnFamily(name.ToString());
            KbLocalCache.Remove($"{rdb.Path}:ColumnFamily:{name}");
        }

        public void DropColumnFamily(RocksDb rdb, RdbKey key)
        {
            rdb.DropColumnFamily(key.ToString());
            KbLocalCache.Remove($"{rdb.Path}:ColumnFamily:{key}");
        }

        internal RocksDb AcquireRdb(string rdbPath)
        {
            return _rdbInstances.GetOrAdd(rdbPath, path =>
            {
                var options = new DbOptions().SetCreateIfMissing(true).SetCreateMissingColumnFamilies(true);
                var cfOptions = new ColumnFamilyOptions();

                var columnFamilies = new ColumnFamilies();
                foreach (var cf in RocksDb.ListColumnFamilies(options, path))
                {
                    columnFamilies.Add(cf, cfOptions);
                }

                return RocksDb.Open(options, path, columnFamilies);
            });
        }

        internal void CreateDocumentsRdb(Transaction? transaction, string rdbPath)
        {
            //TODO: if transaction is not null then we need to write some sort of transaction action to delete this file if the transaction rolls back.

            var options = new DbOptions().SetCreateIfMissing(true).SetCreateMissingColumnFamilies(true);
            var columnFamilyOptions = new ColumnFamilyOptions();
            var columnFamilies = new ColumnFamilies
                {
                    { KbColumnFamily.Documents.ToString(), columnFamilyOptions }, //Document data.
                    { KbColumnFamily.Identity.ToString(), columnFamilyOptions },  //Identity management for auto-incrementing keys, etc.
                    { KbColumnFamily.Indexes.ToString(), columnFamilyOptions },   //Indexes metadata for the documents.
                    { KbColumnFamily.Policy.ToString(), columnFamilyOptions }     //Schema security policies.
                };
            using var rdbInstance = RocksDb.Open(options, rdbPath, columnFamilies);
            rdbInstance.Dispose();

            _core.IO.PutNonTrackedRaw(rdbPath, KbColumnFamily.Identity, new RdbKey(PrimaryIdentityKey), BitConverter.GetBytes(1U));
        }

        internal void CreateSchemaRdb(Transaction? transaction, string rdbPath)
        {
            //TODO: is transaction is not null then we need to write some sort of transaction action to delete this file if the transaction rolls back.

            var options = new DbOptions().SetCreateIfMissing(true).SetCreateMissingColumnFamilies(true);
            var columnFamilyOptions = new ColumnFamilyOptions();
            var columnFamilies = new ColumnFamilies
                {
                    { KbColumnFamily.Schema.ToString(), columnFamilyOptions }, //Child schema definitions.
                    //{ KbColumnFamily.Indexes.ToString(), columnFamilyOptions }, /Indexes are stored in the documents RDB, not the schema RDB.
                    { KbColumnFamily.Procedures.ToString(), columnFamilyOptions }, //Stored procedures.
                    { KbColumnFamily.Identity.ToString(), columnFamilyOptions } //Identity management for auto-incrementing keys, etc.
                };
            using var rdbInstance = RocksDb.Open(options, rdbPath, columnFamilies);
            rdbInstance.Dispose();
        }

        internal bool DoesKeyExist(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, LockOperation intendedOperation)
            => DoesKeyExist(transaction, rdbPath, columnFamilyName, key, intendedOperation, out _);

        internal bool DoesKeyExist(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName,
            RdbKey key, LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey)
        {
            transaction.EnsureActive();
            acquiredLockKey = transaction.LockFile(intendedOperation, rdbPath);
            var rdb = AcquireRdb(rdbPath);
            var columnFamily = GetColumnFamily(rdb, columnFamilyName);

            var bytes = rdb.Get(key.Bytes, columnFamily);

            bool exists = bytes != null;

            return exists;
        }

        internal void DeleteKey(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, RdbKey key)
        {
            transaction.EnsureActive();
            transaction.LockFile(LockOperation.Delete, rdbPath);
            var rdb = AcquireRdb(rdbPath);
            var columnFamily = GetColumnFamily(rdb, columnFamilyName);
            var cacheKey = CacheManager.MakeCacheKey(rdbPath, columnFamilyName, key);

            var originalBytes = rdb.Get(key.Bytes, columnFamily);
            transaction.RecordKeyDelete(rdbPath, columnFamilyName, key, cacheKey, originalBytes);

            rdb.Remove(key.Bytes, columnFamily);
        }

        #endregion

        #region Putters.

        internal void PutNonTrackedButCached<T>(string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, T obj, IOFormat format)
            where T : notnull
        {
            try
            {
                var rdb = AcquireRdb(rdbPath);
                var columnFamily = GetColumnFamily(rdb, columnFamilyName);
                var cacheKey = CacheManager.MakeCacheKey(rdbPath, columnFamilyName, key);

                int approximateSizeInBytes = 0;

                if (format == IOFormat.JSON)
                {
                    string text = JsonConvert.SerializeObject(obj);

                    var bytes = Encoding.UTF8.GetBytes(text);

                    rdb.Put(key.Bytes, bytes, columnFamily);

                    approximateSizeInBytes = bytes.Length;
                }
                else if (format == IOFormat.PBuf)
                {
                    using var output = new MemoryStream();
                    ProtoBuf.Serializer.Serialize(output, obj);

                    rdb.Put(key.Bytes, output.ToArray(), columnFamily);

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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdbPath}].", ex);
                throw;
            }
        }

        internal void PutNonTrackedRaw(string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, byte[] objBytes)
        {
            try
            {
                var rdb = AcquireRdb(rdbPath);
                var columnFamily = GetColumnFamily(rdb, columnFamilyName);
                rdb.Put(key.Bytes, objBytes, columnFamily);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdbPath}].", ex);
                throw;
            }
        }

        internal void PutNonTracked<T>(string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, T obj, IOFormat format)
            where T : notnull
        {
            try
            {
                var rdb = AcquireRdb(rdbPath);
                var columnFamily = GetColumnFamily(rdb, columnFamilyName);

                if (format == IOFormat.JSON)
                {
                    string text = JsonConvert.SerializeObject(obj);
                    var bytes = Encoding.UTF8.GetBytes(text);
                    rdb.Put(key.Bytes, bytes, columnFamily);
                }
                else if (format == IOFormat.PBuf)
                {
                    using var output = new MemoryStream();
                    ProtoBuf.Serializer.Serialize(output, obj);
                    rdb.Put(key.Bytes, output.ToArray(), columnFamily);
                }
                else
                {
                    throw new NotImplementedException($"IO format is not implemented: [{format}].");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{rdbPath}].", ex);
                throw;
            }
        }

        internal void PutJson(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, object obj)
            => InternalTrackedPut(transaction, rdbPath, columnFamilyName, key, obj, LockOperation.Write, IOFormat.JSON);

        internal void PutPBuf(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, object obj)
            => InternalTrackedPut(transaction, rdbPath, columnFamilyName, key, obj, LockOperation.Write, IOFormat.PBuf);

        /// <summary>
        /// Writes to a RDB with transactional tracking, locking and deferred IO, and without caching.
        /// </summary>
        public void InternalTrackedPut<T>(Transaction transaction, string rdbPath, KbColumnFamily columnFamilyName, RdbKey key, T obj, LockOperation? lockOperation, IOFormat format)
            where T : notnull
        {
            try
            {
                transaction.EnsureActive();
                var rdb = AcquireRdb(rdbPath);
                var columnFamily = GetColumnFamily(rdb, columnFamilyName);
                var cacheKey = CacheManager.MakeCacheKey(rdbPath, columnFamilyName, key);
                //var rdbWriteBatch = transaction.AcquireRdbWriteBatch(filePath);

                transaction.LockFile(LockOperation.Write, rdbPath);

                bool doesKeyExist = DoesKeyExist(transaction, rdbPath, columnFamilyName, key, LockOperation.Write, out _);
                if (doesKeyExist)
                {
                    var originalBytes = rdb.Get(key.Bytes, columnFamily);
                    transaction.RecordKeyAlter(rdbPath, columnFamilyName, key, cacheKey, originalBytes);
                }
                else
                {
                    transaction.RecordKeyCreate(rdbPath, columnFamilyName, key, cacheKey);
                }

                if (_core.Settings.DeferredIOEnabled)
                {
                    transaction.DeferredIOs.Write((dio) =>
                    {
                        transaction.Instrumentation.Measure(PerformanceCounter.DeferredWrite, () =>
                            dio.PutDeferredDiskIO(cacheKey, rdbPath, columnFamilyName, obj, key, format));
                    });

                    _core.Health.IncrementDiscrete(HealthCounterType.IODeferredWrites);

                    //We can skip caching because we write this to the deferred IO cache - which
                    //  is infinitely more deterministic than the memory cache auto-ejections.
                    return;
                }

                int approximateSizeInBytes = 0;

                if (format == IOFormat.JSON)
                {
                    string text = transaction.Instrumentation.Measure(PerformanceCounter.Serialize, () =>
                        JsonConvert.SerializeObject(obj));

                    var bytes = Encoding.UTF8.GetBytes(text);

                    transaction.Instrumentation.Measure(PerformanceCounter.IOWrite, () =>
                        rdb.Put(key.Bytes, bytes, columnFamily));

                    approximateSizeInBytes = bytes.Length;

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
                        rdb.Put(key.Bytes, bytes, columnFamily));

                    approximateSizeInBytes = bytes.Length;
                }
                else
                {
                    throw new NotImplementedException($"IO format is not implemented: [{format}].");
                }

                if (_core.Settings.CacheEnabled)
                {
                    transaction.Instrumentation.Measure(PerformanceCounter.CacheWrite, () =>
                        _core.Cache.Set(cacheKey, obj, approximateSizeInBytes));

                    _core.Health.IncrementDiscrete(HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{rdbPath}].", ex);
                throw;
            }
        }

        #endregion
    }
}
