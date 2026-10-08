using Newtonsoft.Json;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.PersistentTypes.Atomicity;
using NTDLS.Katzebase.PersistentTypes.Index;
using NTDLS.Katzebase.PersistentTypes.Schema;
using System.Text;
using static NTDLS.Katzebase.Api.KbConstants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// ATTACH SCHEMA and DETACH SCHEMA: moving whole namespaces (a schema folder with its documents, indexes and child
    /// schemas) into and out of the database.
    ///
    /// A schema's folder is self-contained: it holds the schema's documents/indexes/policies (@documents.kbrdb), its child
    /// schema catalog (@schema.kbrdb) and its child schemas' folders. The only thing outside of it is the entry in the
    /// parent schema's catalog, so attaching is "put the folder in place, then add the catalog entry" and detaching is
    /// the reverse.
    ///
    /// File operations cannot be undone by the transaction log, so both statements are refused inside explicit
    /// transactions, stage their file operations so that a failure never leaves a partially attached/detached
    /// namespace, and register rollback actions in case the transaction is rolled back after the files have moved.
    /// </summary>
    public partial class SchemaManager
    {
        private const string AttachStagingPrefix = ".attaching-";

        /// <summary>
        /// Copies the namespace in [sourceFolder] into the database as the (not yet existing) schema [schemaName].
        /// The source folder is not modified.
        /// </summary>
        internal void Attach(Transaction transaction, string schemaName, string sourceFolder)
        {
            if (transaction.IsUserCreated)
            {
                throw new KbGenericException("Schemas cannot be attached within a user transaction.");
            }

            schemaName = NormalizeTransferSchemaName(schemaName, "attach");
            var sourcePath = NormalizeTransferFolder(sourceFolder);

            if (Directory.Exists(sourcePath) == false)
            {
                throw new KbObjectNotFoundException($"Folder not found: [{sourcePath}].");
            }
            ValidateSchemaFolder(sourcePath, sourcePath);

            //Lock the schema to be attached and its parent.
            var virtualSchema = AcquireVirtual(transaction, schemaName, LockOperation.Write, LockOperation.Write);
            if (virtualSchema.Exists)
            {
                throw new KbObjectAlreadyExistsException($"Schema already exists: [{schemaName}].");
            }

            var parentSchema = virtualSchema.ParentPhysicalSchema;
            var targetPath = virtualSchema.DiskPath;

            if (Directory.Exists(targetPath) || File.Exists(targetPath))
            {
                //A folder without a catalog entry is not a schema, but it would be silently brought back to life by
                //  CREATE SCHEMA, so refuse rather than merge into or overwrite it.
                throw new KbGenericException($"Cannot attach [{schemaName}]: the folder [{targetPath}] already exists but is not a schema. Remove it first.");
            }

            //Copy into a staging folder beside the target, prepare it there, then rename it into place in one step.
            var stagingPath = Path.Combine(parentSchema.DiskPath, $"{AttachStagingPrefix}{Guid.NewGuid():N}");
            AttachReport report;
            try
            {
                CopyDirectory(sourcePath, stagingPath);
                report = PrepareAttachedNamespace(stagingPath, schemaName);
                MoveDirectoryWithRetry(stagingPath, targetPath);
            }
            catch
            {
                TryDeleteDirectory(stagingPath);
                throw;
            }

            try
            {
                //Make sure nothing is served from cache for a schema that may previously have existed at this path.
                _core.Cache.RemoveItemsForPath(new CacheKey(targetPath));

                var physicalSchema = new PhysicalSchema
                {
                    Name = virtualSchema.Name,
                    Id = Guid.NewGuid()
                };

                var parentRdb = _core.IO.AcquireRdb(parentSchema.SchemaFilePath());
                _core.IO.PutJson(transaction, parentRdb, KbColumnFamilyName.Schema, new RdbKey(physicalSchema.Name), physicalSchema);

                //Verify the attached schema can be opened through the engine.
                _core.IO.AcquireDocumentsRdb(Acquire(transaction, schemaName, LockOperation.Write));
            }
            catch
            {
                _core.IO.CloseRdbsUnderPath(targetPath);
                TryDeleteDirectory(targetPath);
                throw;
            }

            //If the transaction is rolled back after this point, the catalog entry is removed by the transaction log
            //  and the attached files must go with it.
            transaction.AddRollbackAction(() =>
            {
                _core.IO.CloseRdbsUnderPath(targetPath);
                TryDeleteDirectory(targetPath);
            });

            transaction.AddMessage($"Attached [{schemaName}] ({report.SchemaCount:N0} schema{(report.SchemaCount == 1 ? "" : "s")}) from [{sourcePath}].", KbMessageType.Verbose);

            foreach (var (indexSchema, indexName) in report.OutdatedIndexes)
            {
                transaction.AddMessage($"Index [{indexName}] on [{indexSchema}] was created by an older version of Katzebase. It is not used by queries,"
                    + $" and inserts, updates and deletes on [{indexSchema}] will fail, until it is rebuilt: REBUILD INDEX {indexName} ON {indexSchema}", KbMessageType.Warning);
            }
        }

        /// <summary>
        /// Removes the schema [schemaName] (with all of its documents, indexes and child schemas) from the database
        /// and moves its folder to [destinationFolder], from where it can be attached to this or another server.
        /// </summary>
        internal void Detach(Transaction transaction, string schemaName, string destinationFolder)
        {
            if (transaction.IsUserCreated)
            {
                throw new KbGenericException("Schemas cannot be detached within a user transaction.");
            }

            schemaName = NormalizeTransferSchemaName(schemaName, "detach");
            var destinationPath = NormalizeTransferFolder(destinationFolder);

            if (Directory.Exists(destinationPath) || File.Exists(destinationPath))
            {
                throw new KbObjectAlreadyExistsException($"The destination folder already exists: [{destinationPath}].");
            }

            var physicalSchema = AcquireVirtual(transaction, schemaName, LockOperation.Write, LockOperation.Write);
            if (physicalSchema.Exists == false)
            {
                throw new KbObjectNotFoundException($"Schema not found: [{schemaName}].");
            }

            var schemaPath = physicalSchema.DiskPath;

            //Wait for every other transaction to finish with anything in (or beneath) the schema.
            transaction.LockPathRecursive(LockOperation.Delete, new CacheKey(schemaPath));

            var parentRdb = _core.IO.AcquireRdb(physicalSchema.ParentPhysicalSchema.SchemaFilePath());
            _core.IO.DeleteKey(transaction, parentRdb, KbColumnFamilyName.Schema, new RdbKey(physicalSchema.Name));

            //Flush and release the schema's files before moving them.
            _core.IO.CloseRdbsUnderPath(schemaPath);

            var destinationParent = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrEmpty(destinationParent) == false)
            {
                Directory.CreateDirectory(destinationParent);
            }

            if (MoveDirectory(schemaPath, destinationPath) == false)
            {
                transaction.AddMessage($"The schema was detached to [{destinationPath}], but its original folder [{schemaPath}] could not be"
                    + " completely removed and should be deleted manually.", KbMessageType.Warning);
            }

            //If the transaction is rolled back after this point, the catalog entry is restored by the transaction log,
            //  so the files must come back too.
            transaction.AddRollbackAction(() =>
            {
                if (Directory.Exists(destinationPath) && Directory.Exists(schemaPath) == false)
                {
                    MoveDirectory(destinationPath, schemaPath);
                }
            });

            _core.Cache.Remove(CacheManager.MakeCacheKey(physicalSchema.ParentPhysicalSchema.SchemaFilePath(), KbColumnFamilyName.Schema, physicalSchema.Name));
            _core.Cache.RemoveItemsForPath(new CacheKey(schemaPath));

            transaction.AddMessage($"Detached [{schemaName}] to [{destinationPath}].", KbMessageType.Verbose);
        }

        #region Helpers.

        private class AttachReport
        {
            public int SchemaCount { get; set; }
            public List<(string Schema, string Index)> OutdatedIndexes { get; } = new();
        }

        private static string NormalizeTransferSchemaName(string schemaName, string operation)
        {
            schemaName = schemaName.Trim().Trim(':').Trim();

            if (schemaName.Length == 0)
            {
                throw new KbInvalidArgumentException($"The root schema cannot be {operation}ed.");
            }
            if (schemaName.StartsWith('#'))
            {
                throw new KbInvalidArgumentException($"Temporary schemas cannot be {operation}ed.");
            }

            return schemaName;
        }

        /// <summary>
        /// Folders must be absolute and must not overlap the server's own data or transaction folders.
        /// </summary>
        private string NormalizeTransferFolder(string folder)
        {
            if (Path.IsPathFullyQualified(folder) == false)
            {
                throw new KbInvalidArgumentException($"The folder path must be absolute: [{folder}].");
            }

            var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));

            foreach (var serverPath in new[] { _core.Settings.DataRootPath, _core.Settings.TransactionDataPath })
            {
                var serverFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(serverPath));
                if (IsSameOrBeneath(fullPath, serverFullPath) || IsSameOrBeneath(serverFullPath, fullPath))
                {
                    throw new KbInvalidArgumentException($"The folder [{fullPath}] cannot be within, or contain, the server's data folder [{serverFullPath}].");
                }
            }

            return fullPath;

            static bool IsSameOrBeneath(string path, string ancestor)
                => path.Equals(ancestor, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(ancestor + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// A schema folder must contain both the documents and the child schema catalog databases.
        /// </summary>
        private static void ValidateSchemaFolder(string folderPath, string displayName)
        {
            if (Directory.Exists(Path.Combine(folderPath, DocumentsFile)) == false
                || Directory.Exists(Path.Combine(folderPath, SchemaFile)) == false)
            {
                throw new KbGenericException($"[{displayName}] is not a Katzebase schema folder, expected it to contain [{DocumentsFile}] and [{SchemaFile}].");
            }
        }

        /// <summary>
        /// Walks a (staged, not yet attached) namespace: verifies that every schema's databases can be opened, gives every
        /// child schema a new id (so that attaching the same folder more than once doesn't produce duplicate schema ids),
        /// and finds indexes stored in an older format.
        /// </summary>
        private static AttachReport PrepareAttachedNamespace(string rootFolder, string rootSchemaName)
        {
            var report = new AttachReport();
            Visit(rootFolder, rootSchemaName);
            return report;

            void Visit(string folderPath, string schemaName)
            {
                ValidateSchemaFolder(folderPath, schemaName);
                report.SchemaCount++;

                using (var documentsRdb = new Rdb(Path.Combine(folderPath, DocumentsFile)))
                {
                    var indexesCF = documentsRdb.GetColumnFamily(KbColumnFamilyName.Indexes);
                    using var iterator = documentsRdb.NewIterator(indexesCF);
                    for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
                    {
                        var physicalIndex = JsonConvert.DeserializeObject<PhysicalIndex>(iterator.StringValue());
                        if (physicalIndex != null && physicalIndex.IsCurrentStorageVersion() == false)
                        {
                            report.OutdatedIndexes.Add((schemaName, physicalIndex.Name));
                        }
                    }
                }

                var childNames = new List<string>();
                using (var schemaRdb = new Rdb(Path.Combine(folderPath, SchemaFile)))
                {
                    var schemaCF = schemaRdb.GetColumnFamily(KbColumnFamilyName.Schema);

                    var childEntries = new List<(byte[] Key, PhysicalSchema Schema)>();
                    using (var iterator = schemaRdb.NewIterator(schemaCF))
                    {
                        for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
                        {
                            var childSchema = JsonConvert.DeserializeObject<PhysicalSchema>(iterator.StringValue())
                                ?? throw new KbGenericException($"The schema catalog of [{schemaName}] contains an invalid entry.");
                            childEntries.Add((iterator.Key(), childSchema));
                        }
                    }

                    foreach (var (key, childSchema) in childEntries)
                    {
                        childSchema.Id = Guid.NewGuid();
                        schemaRdb.Put(key, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(childSchema)), schemaCF);
                        childNames.Add(childSchema.Name);
                    }
                }

                foreach (var childName in childNames)
                {
                    Visit(Path.Combine(folderPath, childName), $"{schemaName}:{childName}");
                }
            }
        }

        private static void CopyDirectory(string sourcePath, string destinationPath)
        {
            Directory.CreateDirectory(destinationPath);

            foreach (var file in Directory.EnumerateFiles(sourcePath))
            {
                File.Copy(file, Path.Combine(destinationPath, Path.GetFileName(file)), overwrite: false);
            }

            foreach (var directory in Directory.EnumerateDirectories(sourcePath))
            {
                CopyDirectory(directory, Path.Combine(destinationPath, Path.GetFileName(directory)));
            }
        }

        /// <summary>
        /// Moves a folder, copying it when the destination is on a different volume. Never leaves the data in neither place:
        /// if a cross-volume copy fails, the partial copy is removed and the source is left intact.
        /// </summary>
        /// <returns>False if the data was moved, but the source folder could not be fully removed afterwards.</returns>
        private static bool MoveDirectory(string sourcePath, string destinationPath)
        {
            if (string.Equals(Path.GetPathRoot(sourcePath), Path.GetPathRoot(destinationPath), StringComparison.OrdinalIgnoreCase))
            {
                MoveDirectoryWithRetry(sourcePath, destinationPath);
                return true;
            }

            try
            {
                CopyDirectory(sourcePath, destinationPath);
            }
            catch
            {
                TryDeleteDirectory(destinationPath);
                throw;
            }

            return TryDeleteDirectory(sourcePath);
        }

        /// <summary>
        /// Renames a folder on the same volume. On Windows, a folder cannot be renamed while any file within it is open, and
        /// files that were just written or closed are commonly opened briefly by other processes (antivirus, search
        /// indexing), so transient failures are retried for a short time.
        /// </summary>
        private static void MoveDirectoryWithRetry(string sourcePath, string destinationPath)
        {
            const int maxAttempts = 10;

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    Directory.Move(sourcePath, destinationPath);
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts && (ex is IOException || ex is UnauthorizedAccessException)
                    && Directory.Exists(sourcePath) && Directory.Exists(destinationPath) == false)
                {
                    Thread.Sleep(100 * attempt);
                }
            }
        }

        private static bool TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Warning($"Failed to delete folder [{path}]: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
