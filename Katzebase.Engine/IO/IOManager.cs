﻿using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Documents;
using Katzebase.Engine.Library;
using Katzebase.PublicLibrary;
using Newtonsoft.Json;
using static Katzebase.Engine.Library.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.IO
{
    //Internal core class methods for locking, reading, writing and managing tasks related to disk I/O.
    internal class IOManager
    {
        private readonly Core core;
        public IOManager(Core core)
        {
            this.core = core;
        }

        #region Getters.

        public T GetJsonNonTracked<T>(string filePath, bool useCompression = true)
        {
            try
            {
                T? result;

                if (core.Settings.UseCompression && useCompression)
                {
                    result = JsonConvert.DeserializeObject<T>(Compression.DecompressToString(File.ReadAllBytes(filePath)));
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
                core.Log.Write($"Failed to get non-tracked json for file {filePath}.", ex);
                throw;
            }
        }

        public long GetDecompressedSizeTracked(string filePath)
        {
            try
            {
                if (core.Settings.UseCompression)
                {
                    return Compression.Decompress(File.ReadAllBytes(filePath)).Length;
                }
                else
                {
                    return (new FileInfo(filePath)).Length;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get non-tracked file length for {filePath}.", ex);
                throw;
            }
        }

        public T GetPBufNonTracked<T>(string filePath)
        {
            try
            {
                if (core.Settings.UseCompression)
                {
                    using var input = new MemoryStream(Compression.Decompress(File.ReadAllBytes(filePath)));
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
                core.Log.Write($"Failed to get non-tracked pbuf for file {filePath}.", ex);
                throw;
            }
        }

        internal T GetJson<T>(Transaction transaction, string filePath, LockOperation intendedOperation, bool useCompression = true)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.JSON, useCompression);

        internal T GetPBuf<T>(Transaction transaction, string filePath, LockOperation intendedOperation, bool useCompression = true)
            => InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.PBuf, useCompression);

        /*
        internal PhysicalDocumentPage GetPBuf<T>(Transaction transaction, string filePath, LockOperation intendedOperation) where T : PhysicalDocumentPage
        {
            return InternalTrackedGet<PhysicalDocumentPage>(transaction, filePath, intendedOperation, IOFormat.PBuf, false);
        }
        */

        internal T InternalTrackedGet<T>(Transaction transaction, string filePath, LockOperation intendedOperation, IOFormat format, bool useCompression = true)
        {
            try
            {
                lock (transaction.SyncObject)
                {
                    transaction.EnsureActive();

                    transaction.LockFile(intendedOperation, filePath);

                    if (core.Settings.DeferredIOEnabled)
                    {
                        KbUtility.EnsureNotNull(transaction.DeferredIOs);
                        var ptDeferredWriteRead = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.DeferredRead);
                        var deferredIOObject = transaction.DeferredIOs.GetDeferredDiskIO<T>(filePath);
                        ptDeferredWriteRead?.StopAndAccumulate();

                        if (deferredIOObject != null)
                        {
                            core.Health.Increment(HealthCounterType.IODeferredIOReads);
                            core.Log.Trace($"IO:CacheHit:{transaction.ProcessId}->{filePath}");
                            return deferredIOObject;
                        }
                    }

                    if (core.Settings.CacheEnabled)
                    {
                        var ptCacheRead = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.CacheRead);
                        var cachedObject = core.Cache.TryGet(filePath);
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
                    int aproximateSizeInBytes = 0;

                    if (format == IOFormat.JSON)
                    {
                        string text = string.Empty;
                        var ptIORead = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.IORead);
                        if (core.Settings.UseCompression && useCompression)
                        {
                            text = Compression.DecompressToString(File.ReadAllBytes(filePath));
                        }
                        else
                        {
                            text = File.ReadAllText(filePath);
                        }
                        aproximateSizeInBytes = text.Length;
                        ptIORead?.StopAndAccumulate();

                        var ptDeserialize = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.Deserialize);
                        deserializedObject = JsonConvert.DeserializeObject<T>(text);
                        ptDeserialize?.StopAndAccumulate();
                    }
                    else if (format == IOFormat.PBuf)
                    {
                        var ptIORead = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.IORead);
                        if (core.Settings.UseCompression && useCompression)
                        {
                            var serializedData = Compression.Decompress(File.ReadAllBytes(filePath));
                            aproximateSizeInBytes = serializedData.Length;
                            using var input = new MemoryStream(serializedData);
                            deserializedObject = ProtoBuf.Serializer.Deserialize<T>(input);
                        }
                        else
                        {
                            using var file = File.OpenRead(filePath);
                            aproximateSizeInBytes = (int)file.Length;
                            deserializedObject = ProtoBuf.Serializer.Deserialize<T>(file);
                        }
                        ptIORead?.StopAndAccumulate();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    if (core.Settings.CacheEnabled && deserializedObject != null)
                    {
                        var ptCacheWrite = transaction.PT?.CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType.CacheWrite);
                        core.Cache.Upsert(filePath, deserializedObject, aproximateSizeInBytes);
                        ptCacheWrite?.StopAndAccumulate();
                        core.Health.Increment(HealthCounterType.IOCacheReadAdditions);
                    }

                    KbUtility.EnsureNotNull(deserializedObject);

                    return deserializedObject;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get tracked file for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        #region Putters.

        internal void PutJsonNonTracked(string filePath, object deserializedObject, bool useCompression = true)
        {
            try
            {
                if (core.Settings.UseCompression && useCompression)
                {
                    File.WriteAllBytes(filePath, Compression.Compress(JsonConvert.SerializeObject(deserializedObject)));
                }
                else
                {
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(deserializedObject));
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to put non-tracked json for file {filePath}.", ex);
                throw;
            }
        }

        internal void PutPBufNonTracked(string filePath, object deserializedObject, bool useCompression = true)
        {
            try
            {
                if (core.Settings.UseCompression && useCompression)
                {
                    using var output = new MemoryStream();
                    ProtoBuf.Serializer.Serialize(output, deserializedObject);
                    File.WriteAllBytes(filePath, Compression.Compress(output.ToArray()));
                }
                else
                {
                    using var file = File.Create(filePath);
                    ProtoBuf.Serializer.Serialize(file, deserializedObject);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to put non-tracked pbuf for file {filePath}.", ex);
                throw;
            }
        }

        internal void PutJson(Transaction transaction, string filePath, object deserializedObject, bool useCompression = true)
            => InternalTrackedPut(transaction, filePath, deserializedObject, IOFormat.JSON, useCompression);

        internal void PutPBuf(Transaction transaction, string filePath, object deserializedObject, bool useCompression = true)
            => InternalTrackedPut(transaction, filePath, deserializedObject, IOFormat.PBuf, useCompression);

        /*
        internal void PutPBuf(Transaction transaction, string filePath, PhysicalDocumentPage physicalDocumentPage)
            => InternalTrackedPut(transaction, filePath, physicalDocumentPage, IOFormat.PBuf, false);
        */

        private void InternalTrackedPut(Transaction transaction, string filePath, object deserializedObject, IOFormat format, bool useCompression = true)
        {
            try
            {
                lock (transaction.SyncObject)
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

                        if (core.Settings.DeferredIOEnabled)
                        {
                            core.Log.Trace($"IO:Write-Deferred:{filePath}");

                            KbUtility.EnsureNotNull(transaction.DeferredIOs);
                            var ptDeferredWrite = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.DeferredWrite);
                            transaction.DeferredIOs.PutDeferredDiskIO(filePath, filePath, deserializedObject, format, useCompression);
                            ptDeferredWrite?.StopAndAccumulate();

                            core.Health.Increment(HealthCounterType.IODeferredIOWrites);

                            return; //We can skip caching because we write this to the deferred IO cache - which is infinitely more deterministic than the memory cache auto-ejections.
                        }
                    }

                    core.Log.Trace($"IO:Write:{filePath}");

                    int aproximateSizeInBytes = 0;

                    if (format == IOFormat.JSON)
                    {
                        var ptSerialize = transaction?.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Serialize);
                        string text = JsonConvert.SerializeObject(deserializedObject);
                        ptSerialize?.StopAndAccumulate();

                        aproximateSizeInBytes = text.Length;

                        if (core.Settings.UseCompression && useCompression)
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
                        var ptSerialize = transaction?.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Serialize);
                        if (core.Settings.UseCompression && useCompression)
                        {
                            using (var output = new MemoryStream())
                            {
                                ProtoBuf.Serializer.Serialize(output, deserializedObject);
                                aproximateSizeInBytes = (int)output.Length;
                                var compressedPbuf = Compression.Compress(output.ToArray());
                                File.WriteAllBytes(filePath, compressedPbuf);
                            }
                        }
                        else
                        {
                            using (var file = File.Create(filePath))
                            {
                                aproximateSizeInBytes = (int)file.Length;
                                ProtoBuf.Serializer.Serialize(file, deserializedObject);
                            }
                        }
                        ptSerialize?.StopAndAccumulate();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    if (core.Settings.CacheEnabled)
                    {
                        var ptCacheWrite = transaction?.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.CacheWrite);
                        core.Cache.Upsert(filePath, deserializedObject, aproximateSizeInBytes);
                        ptCacheWrite?.StopAndAccumulate();
                        core.Health.Increment(HealthCounterType.IOCacheWriteAdditions);
                    }
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to put internal tracked file for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        internal bool DirectoryExists(Transaction transaction, string diskPath, LockOperation intendedOperation)
        {
            try
            {
                transaction.LockDirectory(intendedOperation, diskPath);

                core.Log.Trace($"IO:Exists-Directory:{transaction.ProcessId}->{diskPath}");

                return Directory.Exists(diskPath);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to verify directory for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void CreateDirectory(Transaction transaction, string? diskPath)
        {
            try
            {
                if (diskPath == null)
                {
                    throw new ArgumentNullException(nameof(diskPath));
                }

                transaction.LockDirectory(LockOperation.Write, diskPath);

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
                core.Log.Write($"Failed to create directory for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal bool FileExists(Transaction transaction, string filePath, LockOperation intendedOperation)
        {
            try
            {
                string lowerFilePath = filePath.ToLower();

                KbUtility.EnsureNotNull(transaction.DeferredIOs);

                if (transaction.DeferredIOs.ContainsKey(lowerFilePath))
                {
                    return true; //The file might not yet exist, but its in the cache.
                }

                transaction.LockFile(intendedOperation, lowerFilePath);

                core.Log.Trace($"IO:Exits-File:{transaction.ProcessId}->{filePath}");

                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to verify file for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void DeleteFile(Transaction transaction, string filePath)
        {
            try
            {
                string cacheKey = filePath.ToLower();
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
                core.Log.Write($"Failed to delete file for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void DeletePath(Transaction transaction, string diskPath)
        {
            try
            {
                transaction.LockDirectory(LockOperation.Write, diskPath);

                if (core.Settings.CacheEnabled)
                {
                    var ptCacheWrite = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.CacheWrite);
                    core.Cache.RemoveItemsWithPrefix(diskPath);
                    ptCacheWrite?.StopAndAccumulate();
                }

                transaction.RecordPathDelete(diskPath);

                core.Log.Trace($"IO:Delete-Directory:{transaction.ProcessId}->{diskPath}");

                Directory.Delete(diskPath, true);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete path for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}
