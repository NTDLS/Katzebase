using Newtonsoft.Json;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Engine.Locking;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;
using static NTDLS.Katzebase.Engine.Trace.PerformanceTrace;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    //Internal core class methods for locking, reading, writing and managing tasks related to disk I/O.
    internal class IOManager
    {
        private readonly EngineCore _core;
        public IOManager(EngineCore core)
        {
            _core = core;
        }

        #region Getters.

        public T GetJsonNonTracked<T>(string filePath, bool useCompression = true)
        {
            try
            {
                T? result;

                if (_core.Settings.UseCompression && useCompression)
                {
                    result = JsonConvert.DeserializeObject<T>(Library.Compression.Deflate.DecompressToString(File.ReadAllBytes(filePath)));
                }
                else
                {
                    result = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                }
                KbUtility.EnsureNotNull(result);
                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to get non-tracked json for file {filePath}.", ex);
                throw;
            }
        }

        public long GetDecompressedSizeTracked(string filePath)
        {
            try
            {
                if (_core.Settings.UseCompression)
                {
                    return Library.Compression.Deflate.Decompress(File.ReadAllBytes(filePath)).Length;
                }
                else
                {
                    return new FileInfo(filePath).Length;
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to get non-tracked file length for {filePath}.", ex);
                throw;
            }
        }

        public T GetPBufNonTracked<T>(string filePath)
        {
            try
            {
                if (_core.Settings.UseCompression)
                {
                    using var input = new MemoryStream(Library.Compression.Deflate.Decompress(File.ReadAllBytes(filePath)));
                    return ProtoBuf.Serializer.Deserialize<T>(input);
                }
                else
                {
                    using var file = File.OpenRead(filePath);
                    return ProtoBuf.Serializer.Deserialize<T>(file);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to get non-tracked pbuf for file {filePath}.", ex);
                throw;
            }
        }

        internal T GetJson<T>(Transaction transaction, string filePath, LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey, bool useCompression = true)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.JSON, out acquiredLockKey, useCompression);

        internal T GetPBuf<T>(Transaction transaction, string filePath, LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey, bool useCompression = true)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.PBuf, out acquiredLockKey, useCompression);

        internal T GetPBuf<T>(Transaction transaction, string filePath, LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.PBuf, out acquiredLockKey, false);

        internal T GetJson<T>(Transaction transaction, string filePath, LockOperation intendedOperation, bool useCompression = true)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.JSON, out _, useCompression);

        internal T GetPBuf<T>(Transaction transaction, string filePath, LockOperation intendedOperation, bool useCompression = true)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.PBuf, out _, useCompression);

        internal T GetPBuf<T>(Transaction transaction, string filePath, LockOperation intendedOperation)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.PBuf, out _, false);


        internal T InternalTrackedGet<T>(Transaction transaction, string filePath,
            LockOperation intendedOperation, IOFormat format, out ObjectLockKey? acquiredLockKey, bool useCompression = true)
        {
            try
            {
                ObjectLockKey? internalAcquiredLockKey = null;

                var result = transaction.CriticalSectionTransaction.Write(() =>
                {
                    transaction.EnsureActive();

                    internalAcquiredLockKey = transaction.LockFile(intendedOperation, filePath);

                    transaction.RecordFileRead(filePath);

                    if (_core.Settings.DeferredIOEnabled)
                    {
                        var result = transaction.DeferredIOs.ReadNullable((obj) =>
                        {
                            var ptDeferredWriteRead = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.DeferredRead);
                            bool wasDeferred = obj.GetDeferredDiskIO<T>(filePath, out var reference);
                            ptDeferredWriteRead?.StopAndAccumulate();

                            if (wasDeferred)
                            {
                                _core.Health.Increment(HealthCounterType.IODeferredIOReads);
                                _core.Log.Trace($"IO:CacheHit:{transaction.ProcessId}->{filePath}");

                                return reference;
                            }
                            return default;
                        });

                        if (Helpers.IsNotDefault(result))
                        {
                            return result;
                        }
                    }

                    if (_core.Settings.CacheEnabled)
                    {
                        var ptCacheRead = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.CacheRead);
                        bool cacheHit = _core.Cache.TryGet(filePath, out var cachedObject);
                        ptCacheRead?.StopAndAccumulate();

                        if(cacheHit)
                        {
                            _core.Health.Increment(HealthCounterType.IOCacheReadHits);
                            _core.Log.Trace($"IO:CacheHit:{transaction.ProcessId}->{filePath}");

                            return (T?)cachedObject;
                        }
                    }

                    _core.Health.Increment(HealthCounterType.IOCacheReadMisses);

                    _core.Log.Trace($"IO:Read:{transaction.ProcessId}->{filePath}");

                    T? deserializedObject;
                    int approximateSizeInBytes = 0;

                    if (format == IOFormat.JSON)
                    {
                        string text = string.Empty;
                        var ptIORead = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.IORead);
                        if (_core.Settings.UseCompression && useCompression)
                        {
                            text = Library.Compression.Deflate.DecompressToString(File.ReadAllBytes(filePath));
                        }
                        else
                        {
                            text = File.ReadAllText(filePath);
                        }
                        approximateSizeInBytes = text.Length;
                        ptIORead?.StopAndAccumulate();

                        var ptDeserialize = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.Deserialize);
                        deserializedObject = JsonConvert.DeserializeObject<T>(text);
                        ptDeserialize?.StopAndAccumulate();
                    }
                    else if (format == IOFormat.PBuf)
                    {
                        var ptIORead = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.IORead);
                        if (_core.Settings.UseCompression && useCompression)
                        {
                            var serializedData = Library.Compression.Deflate.Decompress(File.ReadAllBytes(filePath));
                            approximateSizeInBytes = serializedData.Length;
                            using var input = new MemoryStream(serializedData);
                            deserializedObject = ProtoBuf.Serializer.Deserialize<T>(input);
                        }
                        else
                        {
                            using var file = File.OpenRead(filePath);
                            approximateSizeInBytes = (int)file.Length;
                            deserializedObject = ProtoBuf.Serializer.Deserialize<T>(file);
                        }
                        ptIORead?.StopAndAccumulate();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    if (_core.Settings.CacheEnabled && deserializedObject != null)
                    {
                        var ptCacheWrite = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.CacheWrite);
                        _core.Cache.Upsert(filePath, deserializedObject, approximateSizeInBytes);
                        ptCacheWrite?.StopAndAccumulate();
                        _core.Health.Increment(HealthCounterType.IOCacheReadAdditions);
                    }

                    KbUtility.EnsureNotNull(deserializedObject);

                    return deserializedObject;
                });

                KbUtility.EnsureNotNull(result);

                acquiredLockKey = internalAcquiredLockKey;

                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to get tracked file for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        #region Putters.

        internal void PutJsonNonTrackedButCached(string filePath, object deserializedObject, bool useCompression = true)
        {
            try
            {
                string text = JsonConvert.SerializeObject(deserializedObject);

                int approximateSizeInBytes = text.Length;

                if (_core.Settings.UseCompression && useCompression)
                {
                    File.WriteAllBytes(filePath, Library.Compression.Deflate.Compress(text));
                }
                else
                {
                    File.WriteAllText(filePath, text);
                }

                if (_core.Settings.CacheEnabled)
                {
                    _core.Cache.Upsert(filePath, deserializedObject, approximateSizeInBytes);
                    _core.Health.Increment(HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to put non-tracked json for file {filePath}.", ex);
                throw;
            }
        }

        internal void PutJsonNonTracked(string filePath, object deserializedObject, bool useCompression = true)
        {
            try
            {
                if (_core.Settings.UseCompression && useCompression)
                {
                    File.WriteAllBytes(filePath, Library.Compression.Deflate.Compress(JsonConvert.SerializeObject(deserializedObject)));
                }
                else
                {
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(deserializedObject));
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to put non-tracked json for file {filePath}.", ex);
                throw;
            }
        }

        internal void PutPBufNonTrackedButCached(string filePath, object deserializedObject, bool useCompression = true)
        {
            try
            {
                int approximateSizeInBytes = 0;

                if (_core.Settings.UseCompression && useCompression)
                {
                    using (var output = new MemoryStream())
                    {
                        ProtoBuf.Serializer.Serialize(output, deserializedObject);
                        approximateSizeInBytes = (int)output.Length;
                        var compressedPbuf = Library.Compression.Deflate.Compress(output.ToArray());
                        File.WriteAllBytes(filePath, compressedPbuf);
                    }
                }
                else
                {
                    using (var file = File.Create(filePath))
                    {
                        approximateSizeInBytes = (int)file.Length;
                        ProtoBuf.Serializer.Serialize(file, deserializedObject);
                    }
                }

                if (_core.Settings.CacheEnabled)
                {
                    _core.Cache.Upsert(filePath, deserializedObject, approximateSizeInBytes);
                    _core.Health.Increment(HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to put non-tracked pbuf for file {filePath}.", ex);
                throw;
            }
        }

        internal void PutPBufNonTracked(string filePath, object deserializedObject, bool useCompression = true)
        {
            try
            {
                if (_core.Settings.UseCompression && useCompression)
                {
                    using var output = new MemoryStream();
                    ProtoBuf.Serializer.Serialize(output, deserializedObject);
                    File.WriteAllBytes(filePath, Library.Compression.Deflate.Compress(output.ToArray()));
                }
                else
                {
                    using var file = File.Create(filePath);
                    ProtoBuf.Serializer.Serialize(file, deserializedObject);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to put non-tracked pbuf for file {filePath}.", ex);
                throw;
            }
        }

        internal void PutJson(Transaction transaction, string filePath, object deserializedObject, bool useCompression = true)
            => InternalTrackedPut(transaction, filePath, deserializedObject, IOFormat.JSON, useCompression);

        internal void PutPBuf(Transaction transaction, string filePath, object deserializedObject, bool useCompression = true)
            => InternalTrackedPut(transaction, filePath, deserializedObject, IOFormat.PBuf, useCompression);

        internal void PutPBuf(Transaction transaction, string filePath, PhysicalDocumentPage physicalDocumentPage)
            => InternalTrackedPut(transaction, filePath, physicalDocumentPage, IOFormat.PBuf, false);

        private void InternalTrackedPut(Transaction transaction, string filePath, object deserializedObject, IOFormat format, bool useCompression = true)
        {
            try
            {
                transaction.EnsureActive();

                transaction.LockFile(LockOperation.Write, filePath);

                if (transaction != null)
                {
                    bool doesFileExist = File.Exists(filePath);

                    if (doesFileExist == false)
                    {
                        transaction.RecordFileCreate(filePath);
                    }
                    else
                    {
                        transaction.RecordFileAlter(filePath);
                    }

                    if (_core.Settings.DeferredIOEnabled)
                    {
                        _core.Log.Trace($"IO:Write-Deferred:{filePath}");

                        transaction.DeferredIOs.Write((obj) =>
                        {
                            var ptDeferredWrite = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.DeferredWrite);
                            obj.PutDeferredDiskIO(filePath, filePath, deserializedObject, format, useCompression);
                            ptDeferredWrite?.StopAndAccumulate();
                        });

                        _core.Health.Increment(HealthCounterType.IODeferredIOWrites);

                        return; //We can skip caching because we write this to the deferred IO cache - which is infinitely more deterministic than the memory cache auto-ejections.
                    }
                }

                _core.Log.Trace($"IO:Write:{filePath}");

                int approximateSizeInBytes = 0;

                if (format == IOFormat.JSON)
                {
                    var ptSerialize = transaction?.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Serialize);
                    string text = JsonConvert.SerializeObject(deserializedObject);
                    ptSerialize?.StopAndAccumulate();

                    approximateSizeInBytes = text.Length;

                    if (_core.Settings.UseCompression && useCompression)
                    {
                        File.WriteAllBytes(filePath, Library.Compression.Deflate.Compress(text));
                    }
                    else
                    {
                        File.WriteAllText(filePath, text);
                    }
                }
                else if (format == IOFormat.PBuf)
                {
                    var ptSerialize = transaction?.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Serialize);

                    if (_core.Settings.UseCompression && useCompression)
                    {
                        using (var output = new MemoryStream())
                        {
                            ProtoBuf.Serializer.Serialize(output, deserializedObject);
                            approximateSizeInBytes = (int)output.Length;
                            var compressedPbuf = Library.Compression.Deflate.Compress(output.ToArray());
                            File.WriteAllBytes(filePath, compressedPbuf);
                        }
                    }
                    else
                    {
                        using (var file = File.Create(filePath))
                        {
                            approximateSizeInBytes = (int)file.Length;
                            ProtoBuf.Serializer.Serialize(file, deserializedObject);
                        }
                    }
                    ptSerialize?.StopAndAccumulate();
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (_core.Settings.CacheEnabled)
                {
                    var ptCacheWrite = transaction?.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.CacheWrite);
                    _core.Cache.Upsert(filePath, deserializedObject, approximateSizeInBytes);
                    ptCacheWrite?.StopAndAccumulate();
                    _core.Health.Increment(HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to put internal tracked file for process id {transaction?.ProcessId ?? 0}.", ex);
                throw;
            }
        }

        #endregion

        internal bool DirectoryExists(Transaction transaction, string diskPath, LockOperation intendedOperation)
        {
            return DirectoryExists(transaction, diskPath, intendedOperation, out _);
        }

        internal bool DirectoryExists(Transaction transaction, string diskPath, LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                acquiredLockKey = transaction.LockDirectory(intendedOperation, diskPath);

                _core.Log.Trace($"IO:Exists-Directory:{transaction.ProcessId}->{diskPath}");

                return Directory.Exists(diskPath);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to verify directory for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void CreateDirectory(Transaction transaction, string? diskPath)
        {
            CreateDirectory(transaction, diskPath, out _);
        }

        internal void CreateDirectory(Transaction transaction, string? diskPath, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                if (diskPath == null)
                {
                    throw new ArgumentNullException(nameof(diskPath));
                }

                acquiredLockKey = transaction.LockDirectory(LockOperation.Write, diskPath);

                bool doesFileExist = Directory.Exists(diskPath);

                _core.Log.Trace($"IO:Create-Directory:{transaction.ProcessId}->{diskPath}");

                if (doesFileExist == false)
                {
                    Directory.CreateDirectory(diskPath);
                    transaction.RecordDirectoryCreate(diskPath);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to create directory for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal bool FileExists(Transaction transaction, string filePath, LockOperation intendedOperation)
        {
            return FileExists(transaction, filePath, intendedOperation, out _);
        }

        internal bool FileExists(Transaction transaction, string filePath, LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                string lowerFilePath = filePath.ToLower();

                bool result = false;

                transaction.DeferredIOs.Read((obj) =>
                {
                    if (obj.ContainsKey(lowerFilePath))
                    {
                        result = true; //The file might not yet exist, but its in the cache.
                    }
                });

                if (result)
                {
                    acquiredLockKey = null;
                    return result;
                }

                acquiredLockKey = transaction.LockFile(intendedOperation, lowerFilePath);

                _core.Log.Trace($"IO:Exits-File:{transaction.ProcessId}->{filePath}");

                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to verify file for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void DeleteFile(Transaction transaction, string filePath)
        {
            DeleteFile(transaction, filePath, out _);
        }

        internal void DeleteFile(Transaction transaction, string filePath, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                string cacheKey = filePath.ToLower();
                acquiredLockKey = transaction.LockFile(LockOperation.Delete, cacheKey);

                if (_core.Settings.CacheEnabled)
                {
                    var ptCacheWrite = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.CacheWrite);
                    _core.Cache.Remove(cacheKey);
                    ptCacheWrite?.StopAndAccumulate();
                }

                transaction.RecordFileDelete(filePath);

                _core.Log.Trace($"IO:Delete-File:{transaction.ProcessId}->{filePath}");

                File.Delete(filePath);
                Helpers.RemoveDirectoryIfEmpty(Path.GetDirectoryName(filePath));
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to delete file for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void DeletePath(Transaction transaction, string diskPath)
        {
            DeletePath(transaction, diskPath, out _);
        }

        internal void DeletePath(Transaction transaction, string diskPath, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                acquiredLockKey = transaction.LockPath(LockOperation.Delete, diskPath);

                if (_core.Settings.CacheEnabled)
                {
                    var ptCacheWrite = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.CacheWrite);
                    _core.Cache.RemoveItemsWithPrefix(diskPath);
                    ptCacheWrite?.StopAndAccumulate();
                }

                transaction.RecordPathDelete(diskPath);

                _core.Log.Trace($"IO:Delete-Directory:{transaction.ProcessId}->{diskPath}");

                Directory.Delete(diskPath, true);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to delete path for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}
