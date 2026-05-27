using Newtonsoft.Json;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.PersistentTypes.Schema;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to documents.
    /// </summary>
    public class DocumentManager
    {
        private readonly EngineCore _core;
        private readonly object[] _identityLocks = Enumerable.Range(0, 256).Select(_ => new object()).ToArray();

        internal DocumentQueryHandlers QueryHandlers { get; private set; }
        public DocumentAPIHandlers APIHandlers { get; private set; }

        internal DocumentManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new DocumentQueryHandlers(core);
                APIHandlers = new DocumentAPIHandlers(core);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate document manager.", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to read a document we do it here.
        /// Allows for returning null if the document doesn't exist;
        /// </summary>
        internal PhysicalDocument? AcquireDocumentVirtual(
            Transaction transaction, PhysicalSchema physicalSchema, uint documentId, LockOperation lockIntention)
        {
            try
            {
                return _core.IO.GetPBuf<PhysicalDocument>(transaction, physicalSchema.DocumentsFilePath(), KbColumnFamily.Documents, new RdbKey(documentId), lockIntention);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to read a document we do it here.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentId"></param>
        internal PhysicalDocument AcquireDocument(
            Transaction transaction, PhysicalSchema physicalSchema, uint documentId, LockOperation lockIntention)
        {
            try
            {
                return _core.IO.GetPBuf<PhysicalDocument>(transaction, physicalSchema.DocumentsFilePath(), KbColumnFamily.Documents, new RdbKey(documentId), lockIntention)
                    ?? throw new Exception($"Document with ID [{documentId}] does not exist in schema [{physicalSchema.Name}].");
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal IEnumerable<uint> AcquireDocumentPointers(
            Transaction transaction, string schemaName, LockOperation lockIntention, int? maxCount = null)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                return AcquireDocumentPointers(transaction, physicalSchema, lockIntention, maxCount);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal HashSet<uint> AcquireDocumentPointers(
            Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockIntention, int? maxCount = null)
        {
            try
            {
                var rdb = _core.IO.AcquireRdb(physicalSchema.DocumentsFilePath());

                var documentPointers = new HashSet<uint>();

                using var iterator = rdb.NewIterator(_core.IO.GetColumnFamily(rdb, KbColumnFamily.Documents));
                for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
                {
                    if (maxCount != null && documentPointers.Count >= maxCount.Value)
                    {
                        break;
                    }
                    documentPointers.Add(RdbKey.ConvertToUint(iterator.Key()));
                }

                return documentPointers;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal uint InsertDocument(Transaction transaction, string schemaName, object pageContent)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                return InsertDocument(transaction, physicalSchema, JsonConvert.SerializeObject(pageContent));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal uint InsertDocument(Transaction transaction, string schemaName, string pageContent)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                return InsertDocument(transaction, physicalSchema, pageContent);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Fixed memory, low contention at 256 buckets, no cleanup needed. Two different paths can theoretically share
        /// a bucket (hash collision) and serialize against each other, but it's rare and harmless — correctness is maintained either way.
        /// </summary>
        private object GetIdentityLock(string path) => _identityLocks[Math.Abs(path.GetHashCode()) % _identityLocks.Length];

        public uint GetNextIdentity(PhysicalSchema physicalSchema)
        {
            lock (GetIdentityLock(physicalSchema.SchemaFilePath()))
            {
                var bytes = _core.IO.GetNotTrackedRaw(physicalSchema.SchemaFilePath(), KbColumnFamily.Identity, new RdbKey(PrimaryIdentityKey));
                var identity = bytes == null ? 0U : BitConverter.ToUInt32(bytes);
                identity++;
                _core.IO.PutNonTrackedRaw(physicalSchema.SchemaFilePath(), KbColumnFamily.Identity, new RdbKey(PrimaryIdentityKey), BitConverter.GetBytes(identity));
                return identity;
            }
        }

        public uint GetCurrentIdentity(PhysicalSchema physicalSchema)
        {
            lock (GetIdentityLock(physicalSchema.SchemaFilePath()))
            {
                var bytes = _core.IO.GetNotTrackedRaw(physicalSchema.SchemaFilePath(), KbColumnFamily.Identity, new RdbKey(PrimaryIdentityKey));
                var identity = bytes == null ? 0U : BitConverter.ToUInt32(bytes);
                return identity;
            }
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal uint InsertDocument(Transaction transaction, PhysicalSchema physicalSchema, string pageContent)
        {
            try
            {
                //Open the document page catalog:
                //var documentPageCatalog = _core.IO.GetPBuf<PhysicalDocumentPageCatalog>(
                //    transaction, physicalSchema.DocumentPageCatalogFilePath(), LockOperation.Write);
                //uint physicalDocumentId = documentPageCatalog.ConsumeNextDocumentId();

                uint physicalDocumentId = GetNextIdentity(physicalSchema);

                var physicalDocument = new PhysicalDocument(pageContent)
                {
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                };

                _core.IO.PutPBuf(transaction, physicalSchema.DocumentsFilePath(), KbColumnFamily.Documents, new RdbKey(physicalDocumentId), physicalDocument);

                //Update all of the indexes that reference the document.
                _core.Indexes.InsertDocumentIntoIndexes(transaction, physicalSchema, physicalDocument, physicalDocumentId);

                return physicalDocumentId;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to update multiple documents in the same schema, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="updatedDocuments">List of document pointers and their new content.</param>
        internal void UpdateDocuments(Transaction transaction, PhysicalSchema physicalSchema,
            Dictionary<uint, KbInsensitiveDictionary<string?>> updatedDocuments)
        {
            try
            {
                var indexingDocuments = new Dictionary<uint, PhysicalDocument>();
                var modifiedFieldNames = new HashSet<string>();

                foreach (var updatedDocument in updatedDocuments)
                {
                    var physicalDocument = _core.IO.GetPBuf<PhysicalDocument>(transaction, physicalSchema.DocumentsFilePath(), KbColumnFamily.Documents, new RdbKey(updatedDocument.Key), LockOperation.Write)
                        ?? throw new Exception($"Document with ID [{updatedDocument.Key}] does not exist in schema [{physicalSchema.Name}].");

                    physicalDocument.Modified = DateTime.UtcNow;

                    //Update all of the modified values into the document:
                    foreach (var updatedValue in updatedDocument.Value)
                    {
                        physicalDocument.Elements[updatedValue.Key] = updatedValue.Value;
                        modifiedFieldNames.Add(updatedValue.Key);
                    }

                    //Keep track of the modified physical documents for indexing:
                    indexingDocuments.Add(updatedDocument.Key, physicalDocument);

                    //Save the document page:
                    _core.IO.PutPBuf(transaction, physicalSchema.DocumentsFilePath(), KbColumnFamily.Documents, new RdbKey(updatedDocument.Key), physicalDocument);
                }

                //var modifiedFieldNames = documentContent.Select(o=>o.Value);

                //Update all of the indexes that reference the document.
                _core.Indexes.UpdateDocumentsIntoIndexes(transaction, physicalSchema, indexingDocuments, modifiedFieldNames);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to update a document, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void DeleteDocuments(Transaction transaction, PhysicalSchema physicalSchema, IEnumerable<uint> documentIds)
        {
            try
            {
                foreach (var documentId in documentIds)
                {
                    _core.IO.DeleteKey(transaction, physicalSchema.DocumentsFilePath(), KbColumnFamily.Documents, new RdbKey(documentId));

                }

                //Update all of the indexes that reference the documents.
                _core.Indexes.RemoveDocumentsFromIndexes(transaction, physicalSchema, documentIds);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }
    }
}
