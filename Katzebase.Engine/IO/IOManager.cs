﻿using Katzebase.Engine.Transactions;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.IO
{
    public class IOManager
    {
        private Core core;
        public IOManager(Core core)
        {
            this.core = core;
        }

        #region Getters.

        public T GetJsonNonTracked<T>(string filePath)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
        }

        public T GetPBufNonTracked<T>(string filePath)
        {
            using (var file = File.OpenRead(filePath))
            {
                return ProtoBuf.Serializer.Deserialize<T>(file);
            }
        }

        public T GetJson<T>(Transaction transaction, string filePath, LockOperation intendedOperation)
        {
            return InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.JSON);
        }

        public T GetPBuf<T>(Transaction transaction, string filePath, LockOperation intendedOperation)
        {
            return InternalTrackedGet<T>(transaction, filePath, intendedOperation, IOFormat.PBuf);
        }

        public T InternalTrackedGet<T>(Transaction transaction, string filePath, LockOperation intendedOperation, IOFormat format)
        {
            try
            {
                string cacheKey = Helpers.RemoveModFileName(filePath.ToLower());
                transaction.LockFile(intendedOperation, cacheKey);

                if (core.settings.AllowIOCaching)
                {
                    var cachedObject = core.Cache.Get(cacheKey);

                    if (cachedObject != null)
                    {
                        core.Health.Increment(Constants.HealthCounterType.IOCacheReadHits);

                        core.Log.Trace($"IO:CacheHit:{transaction.ProcessId}->{filePath}");

                        return (T)cachedObject.Value;
                    }
                }

                core.Health.Increment(Constants.HealthCounterType.IOCacheReadMisses);

                core.Log.Trace($"IO:Read:{transaction.ProcessId}->{filePath}");

                T deserializedObject;

                if (format == IOFormat.JSON)
                {
                    string text = File.ReadAllText(filePath);
                    deserializedObject = JsonConvert.DeserializeObject<T>(text);
                }
                else if (format == IOFormat.PBuf)
                {
                    using (var file = File.OpenRead(filePath))
                    {
                        deserializedObject = ProtoBuf.Serializer.Deserialize<T>(file);
                        file.Close();
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (core.settings.AllowIOCaching)
                {
                    core.Cache.Upsert(cacheKey, deserializedObject);
                    core.Health.Increment(Constants.HealthCounterType.IOCacheReadAdditions);
                }

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

        public void PutJsonNonTracked(string filePath, object deserializedObject)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(deserializedObject));
        }

        public void PutPBufNonTracked(string filePath, object deserializedObject)
        {
            using (var file = File.Create(filePath))
            {
                ProtoBuf.Serializer.Serialize(file, deserializedObject);
            }
        }

        public void PutJson(Transaction transaction, string filePath, object deserializedObject)
        {
            InternalTrackedPut(transaction, filePath, deserializedObject, IOFormat.JSON);
        }

        public void PutPBuf(Transaction transaction, string filePath, object deserializedObject)
        {
            InternalTrackedPut(transaction, filePath, deserializedObject, IOFormat.PBuf);
        }

        private void InternalTrackedPut(Transaction transaction, string filePath, object deserializedObject, IOFormat format)
        {
            try
            {
                string cacheKey = Helpers.RemoveModFileName(filePath.ToLower());
                transaction.LockFile(Constants.LockOperation.Write, cacheKey);

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

                    if (core.settings.AllowDeferredIO && transaction.IsLongLived)
                    {
                        deferDiskWrite = transaction.DeferredIOs.RecordDeferredDiskIO(cacheKey, filePath, deserializedObject, format);
                    }
                }

                if (deferDiskWrite == false)
                {
                    core.Log.Trace($"IO:Write:{transaction.ProcessId}->{filePath}");

                    if (format == IOFormat.JSON)
                    {
                        string text = JsonConvert.SerializeObject(deserializedObject);
                        File.WriteAllText(filePath, text);
                    }
                    else if (format == IOFormat.PBuf)
                    {
                        using (var file = File.Create(filePath))
                        {
                            ProtoBuf.Serializer.Serialize(file, deserializedObject);
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
                    core.Log.Trace($"IO:Write-Deferred:{transaction.ProcessId}->{filePath}");
                }

                if (core.settings.AllowIOCaching)
                {
                    core.Cache.Upsert(cacheKey, deserializedObject);
                    core.Health.Increment(Constants.HealthCounterType.IOCacheWriteAdditions);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to put JSON file for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        public bool DirectoryExists(Transaction transaction, string diskPath, LockOperation intendedOperation)
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

        public void CreateDirectory(Transaction transaction, string? diskPath)
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

        public bool FileExists(Transaction transaction, string filePath, LockOperation intendedOperation)
        {
            try
            {
                string lowerFilePath = filePath.ToLower();

                var deferredExists = transaction.DeferredIOs.Collection.Values.FirstOrDefault(o => o.LowerDiskPath == lowerFilePath);
                if (deferredExists != null)
                {
                    //The file might not yet exist, but its in the cache.
                    return true;
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

        public void DeleteFile(Transaction transaction, string filePath)
        {
            try
            {
                string cacheKey = Helpers.RemoveModFileName(filePath.ToLower());
                transaction.LockFile(Constants.LockOperation.Write, cacheKey);

                if (core.settings.AllowIOCaching)
                {
                    core.Cache.Remove(cacheKey);
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

        public void DeletePath(Transaction transaction, string diskPath)
        {
            try
            {
                string cacheKey = Helpers.RemoveModFileName(diskPath.ToLower());
                transaction.LockDirectory(Constants.LockOperation.Write, cacheKey);

                if (core.settings.AllowIOCaching)
                {
                    core.Cache.RemoveStartsWith(cacheKey);
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
