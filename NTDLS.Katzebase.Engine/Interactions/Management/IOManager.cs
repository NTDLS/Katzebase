using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Locking;
using System.Diagnostics;
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

        internal static long GetDecompressedSizeTracked(string filePath)
        {
            try
            {
                return Shared.Compression.Deflate.Decompress(File.ReadAllBytes(filePath)).Length;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{filePath}].", ex);
                throw;
            }
        }

        internal T GetJson<T>(Transaction transaction, string filePath, LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.JSON, out acquiredLockKey);

        internal T GetPBuf<T>(Transaction transaction, string filePath, LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.PBuf, out acquiredLockKey);

        internal T GetJson<T>(Transaction transaction, string filePath, LockOperation intendedOperation)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.JSON, out _);

        internal T GetPBuf<T>(Transaction transaction, string filePath, LockOperation intendedOperation)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.PBuf, out _);

        protected T InternalTrackedGet<T>(Transaction transaction, string filePath,
            LockOperation intendedOperation, IOFormat format, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                acquiredLockKey = transaction.LockFile(intendedOperation, filePath);
                transaction.RecordFileRead(filePath);

                if (_core.Settings.DeferredIOEnabled)
                {
                    var result = transaction.DeferredIOs.ReadNullable((obj) =>
                    {
                        var ptDeferredWriteRead = transaction.Instrumentation.CreateToken(PerformanceCounter.DeferredRead);
                        bool wasDeferred = obj.GetDeferredDiskIO<T>(filePath, out var deferredReference);
                        ptDeferredWriteRead?.StopAndAccumulate();

                        if (wasDeferred)
                        {
                            _core.Health.IncrementDiscrete(HealthCounterType.IODeferredReads);
                            LogManager.Trace($"IO:CacheHit:{transaction.ProcessId}->{filePath}");
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
                    bool cacheHit = _core.Cache.TryGet(filePath, out var cachedObject);
                    ptCacheRead?.StopAndAccumulate();

                    if (cacheHit && cachedObject != null)
                    {
                        _core.Health.IncrementDiscrete(HealthCounterType.IOCacheReadHits);
                        LogManager.Trace($"IO:CacheHit:{transaction.ProcessId}->{filePath}");

                        return (T)cachedObject;
                    }
                }

                _core.Health.IncrementDiscrete(HealthCounterType.IOCacheReadMisses);
                LogManager.Trace($"IO:Read:{transaction.ProcessId}->{filePath}");

                T? deserializedObject;
                int approximateSizeInBytes = 0;

                if (format == IOFormat.JSON)
                {
                    var text = transaction.Instrumentation.Measure<string, T>(PerformanceCounter.IORead, () =>
                        File.ReadAllText(filePath) ?? string.Empty);

                    approximateSizeInBytes = text.Length;

                    deserializedObject = transaction.Instrumentation.Measure(PerformanceCounter.Deserialize, () =>
                        JsonConvert.DeserializeObject<T>(text));

                    //Console.WriteLine($"{ptIORead?.Duration:n0}: '{filePath}'");
                }
                else if (format == IOFormat.PBuf)
                {
                    var fileBytes = transaction.Instrumentation.Measure(PerformanceCounter.IORead,
                        () => File.ReadAllBytes(filePath));

                    var serializedData = transaction.Instrumentation.Measure(PerformanceCounter.Decompress,
                        () => Shared.Compression.Deflate.Decompress(fileBytes));

                    approximateSizeInBytes = serializedData.Length;

                    deserializedObject = transaction.Instrumentation.Measure(PerformanceCounter.Deserialize, () =>
                    {
                        using var input = new MemoryStream(serializedData);
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
                        _core.Cache.Upsert(filePath, deserializedObject, approximateSizeInBytes));

                    _core.Health.IncrementDiscrete(HealthCounterType.IOCacheReadAdditions);
                }

                return deserializedObject.EnsureNotNull();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{filePath}].", ex);
                throw;
            }
        }

        #endregion

        #region Putters.

        internal void PutJsonNonTrackedButCached(string filePath, object deserializedObject)
        {
            try
            {
                string text = JsonConvert.SerializeObject(deserializedObject);

                int approximateSizeInBytes = text.Length;

                File.WriteAllText(filePath, text);

                if (_core.Settings.CacheEnabled)
                {
                    _core.Cache.Upsert(filePath, deserializedObject, approximateSizeInBytes);
                    _core.Health.IncrementDiscrete(HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{filePath}].", ex);
                throw;
            }
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

        internal static void PutJsonNonTracked(string filePath, object deserializedObject)
        {
            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(deserializedObject));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{filePath}].", ex);
                throw;
            }
        }

        internal void PutPBufNonTrackedButCached(string filePath, object deserializedObject)
        {
            try
            {
                int approximateSizeInBytes = 0;

                using var output = new MemoryStream();
                ProtoBuf.Serializer.Serialize(output, deserializedObject);
                approximateSizeInBytes = (int)output.Length;
                var compressedBytes = Shared.Compression.Deflate.Compress(output.ToArray());
                File.WriteAllBytes(filePath, compressedBytes);

                if (_core.Settings.CacheEnabled)
                {
                    _core.Cache.Upsert(filePath, deserializedObject, approximateSizeInBytes);
                    _core.Health.IncrementDiscrete(HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for file: [{filePath}].", ex);
                throw;
            }
        }

        internal static void PutPBufNonTracked(string filePath, object deserializedObject)
        {
            try
            {
                using var output = new MemoryStream();
                ProtoBuf.Serializer.Serialize(output, deserializedObject);
                File.WriteAllBytes(filePath, Shared.Compression.Deflate.Compress(output.ToArray()));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()}  failed for file: [ {filePath} ].", ex);
                throw;
            }
        }

        internal void PutJson(Transaction transaction, string filePath, object deserializedObject)
            => InternalTrackedPut(transaction, filePath, deserializedObject, IOFormat.JSON);

        internal void PutPBuf(Transaction transaction, string filePath, object deserializedObject)
            => InternalTrackedPut(transaction, filePath, deserializedObject, IOFormat.PBuf);

        protected void InternalTrackedPut(Transaction transaction, string filePath, object deserializedObject, IOFormat format)
        {
            try
            {
                transaction.EnsureActive();

                transaction.LockFile(LockOperation.Write, filePath);

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
                    transaction.DeferredIOs.Write((obj) =>
                    {
                        transaction.Instrumentation.Measure(PerformanceCounter.DeferredWrite, () =>
                            obj.PutDeferredDiskIO(filePath, filePath, deserializedObject, format));
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
                        JsonConvert.SerializeObject(deserializedObject));

                    approximateSizeInBytes = text.Length;

                    transaction.Instrumentation.Measure(PerformanceCounter.IOWrite, () =>
                        File.WriteAllText(filePath, text));
                }
                else if (format == IOFormat.PBuf)
                {
                    var bytes = transaction.Instrumentation.Measure(PerformanceCounter.Serialize, () =>
                        {
                            using var output = new MemoryStream();
                            ProtoBuf.Serializer.Serialize(output, deserializedObject);
                            return output.ToArray();
                        });

                    approximateSizeInBytes = bytes.Length;

                    var compressedBytes = transaction.Instrumentation.Measure(PerformanceCounter.Compress, () =>
                        Shared.Compression.Deflate.Compress(bytes));

                    transaction.Instrumentation.Measure(PerformanceCounter.IOWrite, () =>
                        File.WriteAllBytes(filePath, compressedBytes));
                }
                else
                {
                    throw new NotImplementedException($"IO format is not implemented: [{format}].");
                }

                if (_core.Settings.CacheEnabled)
                {
                    transaction.Instrumentation.Measure(PerformanceCounter.CacheWrite, () =>
                        _core.Cache.Upsert(filePath, deserializedObject, approximateSizeInBytes));

                    _core.Health.IncrementDiscrete(HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{filePath}].", ex);
                throw;
            }
        }

        #endregion

        internal static bool DirectoryExists(Transaction transaction, string diskPath, LockOperation intendedOperation)
        {
            return DirectoryExists(transaction, diskPath, intendedOperation, out _);
        }

        internal static bool DirectoryExists(Transaction transaction, string diskPath,
            LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                acquiredLockKey = transaction.LockDirectory(intendedOperation, diskPath);

                return Directory.Exists(diskPath);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{diskPath}].", ex);
                throw;
            }
        }

        internal static void CreateDirectory(Transaction transaction, string? diskPath)
        {
            CreateDirectory(transaction, diskPath, out _);
        }

        internal static void CreateDirectory(Transaction transaction, string? diskPath, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(diskPath);

                acquiredLockKey = transaction.LockDirectory(LockOperation.Write, diskPath);

                bool doesFileExist = Directory.Exists(diskPath);

                if (doesFileExist == false)
                {
                    Directory.CreateDirectory(diskPath);
                    transaction.RecordDirectoryCreate(diskPath);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{diskPath}].", ex);
                throw;
            }
        }

        internal static bool FileExists(Transaction transaction, string filePath, LockOperation intendedOperation)
        {
            return FileExists(transaction, filePath, intendedOperation, out _);
        }

        internal static bool FileExists(Transaction transaction, string filePath, LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                string lowerFilePath = filePath.ToLowerInvariant();

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

                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{filePath}].", ex);
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
                string cacheKey = filePath.ToLowerInvariant();
                acquiredLockKey = transaction.LockFile(LockOperation.Delete, cacheKey);

                if (_core.Settings.CacheEnabled)
                {
                    transaction.Instrumentation.Measure(PerformanceCounter.CacheWrite, () =>
                        _core.Cache.Remove(cacheKey));
                }

                transaction.RecordFileDelete(filePath);

                File.Delete(filePath);
                Shared.Helpers.RemoveDirectoryIfEmpty(Path.GetDirectoryName(filePath));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{filePath}].", ex);
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
                    transaction.Instrumentation.Measure(PerformanceCounter.CacheWrite, () =>
                        _core.Cache.RemoveItemsWithPrefix(diskPath));
                }

                transaction.RecordPathDelete(diskPath);

                Directory.Delete(diskPath, true);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], file: [{diskPath}].", ex);
                throw;
            }
        }
    }
}
