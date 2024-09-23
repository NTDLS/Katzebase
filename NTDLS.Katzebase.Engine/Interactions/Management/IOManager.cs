using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Locking;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

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

        internal T GetJsonNonTracked<T>(string filePath)
        {
            LogManager.Debug($"IO:Read:{filePath}");

            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath)).EnsureNotNull();
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to get non-tracked json for file {filePath}.", ex);
                throw;
            }
        }

        internal long GetDecompressedSizeTracked(string filePath)
        {
            LogManager.Debug($"IO:Read:{filePath}");

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
                LogManager.Error($"Failed to get non-tracked file length for {filePath}.", ex);
                throw;
            }
        }

        internal T GetPBufNonTracked<T>(string filePath)
        {
            LogManager.Debug($"IO:Read:{filePath}");

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
                LogManager.Error($"Failed to get non-tracked pbuf for file {filePath}.", ex);
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
                    if (_core.Settings.UseCompression)
                    {
                        var fileBytes = transaction.Instrumentation.Measure(PerformanceCounter.IORead,
                            () => File.ReadAllBytes(filePath));

                        var serializedData = transaction.Instrumentation.Measure(PerformanceCounter.Decompress,
                            () => Library.Compression.Deflate.Decompress(fileBytes));

                        approximateSizeInBytes = serializedData.Length;

                        deserializedObject = transaction.Instrumentation.Measure(PerformanceCounter.Deserialize, () =>
                        {
                            using var input = new MemoryStream(serializedData);
                            return ProtoBuf.Serializer.Deserialize<T>(input);
                        });
                    }
                    else
                    {
                        var serializedData = transaction.Instrumentation.Measure(PerformanceCounter.IORead,
                            () => File.ReadAllBytes(filePath));

                        approximateSizeInBytes = serializedData.Length;

                        deserializedObject = transaction.Instrumentation.Measure(PerformanceCounter.Deserialize, () =>
                        {
                            using var input = new MemoryStream(serializedData);
                            return ProtoBuf.Serializer.Deserialize<T>(input);
                        });
                    }
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
                LogManager.Error($"Failed to get tracked file for process id {transaction.ProcessId}.", ex);
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
                LogManager.Error($"Failed to put non-tracked json for file {filePath}.", ex);
                throw;
            }
        }

        internal void PutJsonNonTrackedPretty(string filePath, object deserializedObject)
        {
            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(deserializedObject, Formatting.Indented));
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to put non-tracked json for file {filePath}.", ex);
                throw;
            }
        }

        internal void PutJsonNonTracked(string filePath, object deserializedObject)
        {
            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(deserializedObject));
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to put non-tracked json for file {filePath}.", ex);
                throw;
            }
        }

        internal void PutPBufNonTrackedButCached(string filePath, object deserializedObject)
        {
            try
            {
                int approximateSizeInBytes = 0;

                if (_core.Settings.UseCompression)
                {
                    using var output = new MemoryStream();
                    ProtoBuf.Serializer.Serialize(output, deserializedObject);
                    approximateSizeInBytes = (int)output.Length;
                    var compressedBytes = Library.Compression.Deflate.Compress(output.ToArray());
                    File.WriteAllBytes(filePath, compressedBytes);
                }
                else
                {
                    using var file = File.Create(filePath);
                    ProtoBuf.Serializer.Serialize(file, deserializedObject);
                    approximateSizeInBytes = (int)file.Length;
                }

                if (_core.Settings.CacheEnabled)
                {
                    _core.Cache.Upsert(filePath, deserializedObject, approximateSizeInBytes);
                    _core.Health.IncrementDiscrete(HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to put non-tracked pbuf for file {filePath}.", ex);
                throw;
            }
        }

        internal void PutPBufNonTracked(string filePath, object deserializedObject)
        {
            try
            {
                if (_core.Settings.UseCompression)
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
                LogManager.Error($"Failed to put non-tracked pbuf for file {filePath}.", ex);
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
                    LogManager.Debug($"IO:Write-Deferred:{filePath}");

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

                LogManager.Debug($"IO:Write:{filePath}");

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
                    if (_core.Settings.UseCompression)
                    {
                        var bytes = transaction.Instrumentation.Measure(PerformanceCounter.Serialize, () =>
                            {
                                using var output = new MemoryStream();
                                ProtoBuf.Serializer.Serialize(output, deserializedObject);
                                return output.ToArray();
                            });

                        approximateSizeInBytes = bytes.Length;

                        var compressedBytes = transaction.Instrumentation.Measure(PerformanceCounter.Compress, () =>
                            Library.Compression.Deflate.Compress(bytes));

                        transaction.Instrumentation.Measure(PerformanceCounter.IOWrite, () =>
                            File.WriteAllBytes(filePath, compressedBytes));
                    }
                    else
                    {
                        using var file = File.Create(filePath);
                        approximateSizeInBytes = (int)file.Length;
                        ProtoBuf.Serializer.Serialize(file, deserializedObject);
                    }
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
                LogManager.Error($"Failed to put internal tracked file for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        internal bool DirectoryExists(Transaction transaction, string diskPath, LockOperation intendedOperation)
        {
            return DirectoryExists(transaction, diskPath, intendedOperation, out _);
        }

        internal bool DirectoryExists(Transaction transaction, string diskPath,
            LockOperation intendedOperation, out ObjectLockKey? acquiredLockKey)
        {
            try
            {
                acquiredLockKey = transaction.LockDirectory(intendedOperation, diskPath);

                LogManager.Debug($"IO:Exists-Directory:{transaction.ProcessId}->{diskPath}");

                return Directory.Exists(diskPath);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to verify directory for process id {transaction.ProcessId}.", ex);
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
                ArgumentNullException.ThrowIfNull(diskPath);

                acquiredLockKey = transaction.LockDirectory(LockOperation.Write, diskPath);

                bool doesFileExist = Directory.Exists(diskPath);

                LogManager.Debug($"IO:Create-Directory:{transaction.ProcessId}->{diskPath}");

                if (doesFileExist == false)
                {
                    Directory.CreateDirectory(diskPath);
                    transaction.RecordDirectoryCreate(diskPath);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to create directory for process id {transaction.ProcessId}.", ex);
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

                LogManager.Debug($"IO:Exists-File:{transaction.ProcessId}->{filePath}");

                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to verify file for process id {transaction.ProcessId}.", ex);
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

                LogManager.Debug($"IO:Delete-File:{transaction.ProcessId}->{filePath}");

                File.Delete(filePath);
                Library.Helpers.RemoveDirectoryIfEmpty(Path.GetDirectoryName(filePath));
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to delete file for process id {transaction.ProcessId}.", ex);
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

                LogManager.Debug($"IO:Delete-Directory:{transaction.ProcessId}->{diskPath}");

                Directory.Delete(diskPath, true);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to delete path for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}
