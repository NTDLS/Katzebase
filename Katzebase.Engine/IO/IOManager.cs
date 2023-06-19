using Katzebase.Engine.Atomicity;
using Katzebase.Engine.KbLib;
using Katzebase.PublicLibrary;
using Newtonsoft.Json;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.IO
{
    internal class IOManager
    {
        private readonly Core core;
        public IOManager(Core core)
        {
            this.core = core;
        }

        #region Getters.

        public T GetJsonNonTracked<T>(string filePath, bool skipCompression = false)
        {
            T? result;

            if (core.Settings.UseCompression && skipCompression == false)
            {
                result = JsonConvert.DeserializeObject<T>(Compression.DecompressString(File.ReadAllBytes(filePath)));
            }
            else
            {
                result = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
            }
            Utility.EnsureNotNull(result);
            return result;
        }

        public T GetPBufNonTracked<T>(string filePath)
        {
            using (var file = File.OpenRead(filePath))
            {
                return ProtoBuf.Serializer.Deserialize<T>(file);
            }
        }

        internal T GetJson<T>(Transaction transaction, string filePath, LockOperation intendedOperation)
        {
            return InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.JSON);
        }

        internal T GetPBuf<T>(Transaction transaction, string filePath, LockOperation intendedOperation)
        {
            return InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.PBuf);
        }

        internal T InternalTrackedGet<T>(Transaction transaction, string filePath, LockOperation intendedOperation, IOFormat format)
        {
            try
            {
                string cacheKey = Helpers.RemoveModFileName(filePath.ToLower());

                transaction.LockFile(intendedOperation, cacheKey);

                if (core.Settings.CacheEnabled)
                {
                    var ptCacheRead = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.CacheRead);
                    var cachedObject = core.Cache.Get(cacheKey);
                    ptCacheRead?.StopAndAccumulate();

                    if (cachedObject != null)
                    {
                        core.Health.Increment(HealthCounterType.IOCacheReadHits);

                        core.Log.Trace($"IO:CacheHit:{transaction.ProcessId}->{filePath}");

                        return (T)cachedObject;
                    }
                }

                core.Health.Increment(HealthCounterType.IOCacheReadMisses);

                core.Log.Trace($"IO:Read:{transaction.ProcessId}->{filePath}");

                T? deserializedObject;

                if (format == IOFormat.JSON)
                {
                    string text = string.Empty;

                    var ptIORead = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.IORead);
                    if (core.Settings.UseCompression)
                    {
                        text = Compression.DecompressString(File.ReadAllBytes(filePath));
                    }
                    else
                    {
                        text = File.ReadAllText(filePath);
                    }
                    ptIORead?.StopAndAccumulate();

                    var ptDeserialize = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.Deserialize);
                    deserializedObject = JsonConvert.DeserializeObject<T>(text);
                    ptDeserialize?.StopAndAccumulate();
                }
                else if (format == IOFormat.PBuf)
                {
                    var ptIORead = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.IORead);
                    using (var file = File.OpenRead(filePath))
                    {
                        ptIORead?.StopAndAccumulate();
                        var ptDeserialize = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.Deserialize);
                        deserializedObject = ProtoBuf.Serializer.Deserialize<T>(file);
                        ptDeserialize?.StopAndAccumulate();
                        file.Close();
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (core.Settings.CacheEnabled && deserializedObject != null)
                {
                    var ptCacheWrite = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.CacheWrite);
                    core.Cache.Upsert(cacheKey, deserializedObject);
                    ptCacheWrite?.StopAndAccumulate();
                    core.Health.Increment(HealthCounterType.IOCacheReadAdditions);
                }

                Utility.EnsureNotNull(deserializedObject);

                return deserializedObject;
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to get JSON object.", ex);
                throw;
            }
        }

        #endregion

        #region Putters.

        internal void PutJsonNonTracked(string filePath, object deserializedObject, bool skipCompression = false)
        {
            if (core.Settings.UseCompression && skipCompression == false)
            {
                File.WriteAllBytes(filePath, Compression.Compress(JsonConvert.SerializeObject(deserializedObject)));
            }
            else
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(deserializedObject));
            }
        }

        internal void PutPBufNonTracked(string filePath, object deserializedObject)
        {
            using (var file = File.Create(filePath))
            {
                ProtoBuf.Serializer.Serialize(file, deserializedObject);
            }
        }

        internal void PutJson(Transaction transaction, string filePath, object deserializedObject)
        {
            InternalTrackedPut(transaction, filePath, deserializedObject, IOFormat.JSON);
        }

        internal void PutPBuf(Transaction transaction, string filePath, object deserializedObject)
        {
            InternalTrackedPut(transaction, filePath, deserializedObject, IOFormat.PBuf);
        }

        private void InternalTrackedPut(Transaction transaction, string filePath, object deserializedObject, IOFormat format)
        {
            try
            {
                string cacheKey = Helpers.RemoveModFileName(filePath.ToLower());
                transaction.LockFile(LockOperation.Write, cacheKey);

                bool deferDiskWrite = false;

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

                    if (core.Settings.DeferredIOEnabled && transaction.IsUserCreated)
                    {
                        Utility.EnsureNotNull(transaction.DeferredIOs);
                        var ptDeferredWrite = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.DeferredWrite);
                        deferDiskWrite = transaction.DeferredIOs.RecordDeferredDiskIO(cacheKey, filePath, deserializedObject, format);
                        ptDeferredWrite?.StopAndAccumulate();
                    }
                }

                if (deferDiskWrite == false)
                {
                    core.Log.Trace($"IO:Write:{filePath}");

                    if (format == IOFormat.JSON)
                    {
                        var ptSerialize = transaction?.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Serialize);
                        string text = JsonConvert.SerializeObject(deserializedObject);
                        ptSerialize?.StopAndAccumulate();

                        if (core.Settings.UseCompression)
                        {
                            File.WriteAllBytes(filePath, Compression.Compress(text));
                        }
                        else
                        {
                            File.WriteAllText(filePath, text);
                        }
                    }
                    else if (format == IOFormat.PBuf)
                    {
                        using (var file = File.Create(filePath))
                        {
                            var ptSerialize = transaction?.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Serialize);
                            ProtoBuf.Serializer.Serialize(file, deserializedObject);
                            ptSerialize?.StopAndAccumulate();
                            file.Close();
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    core.Log.Trace($"IO:Write-Deferred:{filePath}");
                }

                if (core.Settings.CacheEnabled)
                {
                    var ptCacheWrite = transaction?.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.CacheWrite);
                    core.Cache.Upsert(cacheKey, deserializedObject);
                    ptCacheWrite?.StopAndAccumulate();
                    core.Health.Increment(HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to put JSON file for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        internal bool DirectoryExists(Transaction transaction, string diskPath, LockOperation intendedOperation)
        {
            try
            {
                string cacheKey = Helpers.RemoveModFileName(diskPath.ToLower());
                transaction.LockDirectory(intendedOperation, cacheKey);

                core.Log.Trace($"IO:Exists-Directory:{transaction.ProcessId}->{diskPath}");

                return Directory.Exists(diskPath);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to verify directory for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void CreateDirectory(Transaction transaction, string? diskPath)
        {
            if (diskPath == null)
            {
                throw new ArgumentNullException(nameof(diskPath));
            }

            try
            {
                string cacheKey = Helpers.RemoveModFileName(diskPath.ToLower());
                transaction.LockDirectory(LockOperation.Write, cacheKey);

                bool doesFileExist = Directory.Exists(diskPath);

                core.Log.Trace($"IO:Create-Directory:{transaction.ProcessId}->{diskPath}");

                if (doesFileExist == false)
                {
                    Directory.CreateDirectory(diskPath);
                    transaction.RecordDirectoryCreate(diskPath);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create directory for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal bool FileExists(Transaction transaction, string filePath, LockOperation intendedOperation)
        {
            try
            {
                string lowerFilePath = filePath.ToLower();

                Utility.EnsureNotNull(transaction.DeferredIOs);

                if (transaction.DeferredIOs.Collection.Values.Any(o => o.DiskPath == lowerFilePath))
                {
                    return true; //The file might not yet exist, but its in the cache.
                }

                string cacheKey = Helpers.RemoveModFileName(lowerFilePath);
                transaction.LockFile(intendedOperation, cacheKey);

                core.Log.Trace($"IO:Exits-File:{transaction.ProcessId}->{filePath}");

                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to verify file for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void DeleteFile(Transaction transaction, string filePath)
        {
            try
            {
                string cacheKey = Helpers.RemoveModFileName(filePath.ToLower());
                transaction.LockFile(LockOperation.Write, cacheKey);

                if (core.Settings.CacheEnabled)
                {
                    var ptCacheWrite = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.CacheWrite);
                    core.Cache.Remove(cacheKey);
                    ptCacheWrite?.StopAndAccumulate();
                }

                transaction.RecordFileDelete(filePath);

                core.Log.Trace($"IO:Delete-File:{transaction.ProcessId}->{filePath}");

                File.Delete(filePath);
                Helpers.RemoveDirectoryIfEmpty(Path.GetDirectoryName(filePath));
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete file for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void DeletePath(Transaction transaction, string diskPath)
        {
            try
            {
                string cacheKey = Helpers.RemoveModFileName(diskPath.ToLower());
                transaction.LockDirectory(LockOperation.Write, cacheKey);

                if (core.Settings.CacheEnabled)
                {
                    var ptCacheWrite = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.CacheWrite);
                    core.Cache.RemoveItemsWithPrefix(cacheKey);
                    ptCacheWrite?.StopAndAccumulate();
                }

                transaction.RecordPathDelete(diskPath);

                core.Log.Trace($"IO:Delete-Directory:{transaction.ProcessId}->{diskPath}");

                Directory.Delete(diskPath, true);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete path for process {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}
