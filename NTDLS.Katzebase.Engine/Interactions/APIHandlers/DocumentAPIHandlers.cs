using NTDLS.Katzebase.Api.Models;
using Newtonsoft.Json;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers;
using NTDLS.ReliableMessaging;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to documents.
    /// </summary>
    public class DocumentAPIHandlers : IRmMessageHandler
    {
        private readonly EngineCore _core;

        public DocumentAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate document API handlers.", ex);
                throw;
            }
        }

        public KbQueryDocumentSampleReply DocumentSample(RmContext context, KbQueryDocumentSample param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Read);

                #endregion

                var nativeResults = StaticSearcherProcessor.SampleSchemaDocuments(_core, transactionReference.Transaction, param.Schema, param.Count);

                var apiResults = new KbQueryDocumentSampleReply()
                {
                    Rows = nativeResults.Rows,
                    Fields = nativeResults.Fields
                };

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults, apiResults.Rows.Count);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns all documents in a schema with their values.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schemaName"></param>
        /// <param name="rowLimit"></param>
        /// <returns></returns>
        public KbQueryDocumentListReply DocumentList(RmContext context, KbQueryDocumentList param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Read);

                #endregion

                var nativeResults = StaticSearcherProcessor.ListSchemaDocuments(
                    _core, transactionReference.Transaction, param.Schema, param.Count);

                var apiResults = new KbQueryDocumentListReply()
                {
                    Rows = nativeResults.Rows,
                    Fields = nativeResults.Fields
                };

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults, apiResults.Rows.Count);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Saves a new document, this is used for inserts.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        /// <param name="newId"></param>
        /// <exception cref="KbObjectNotFoundException"></exception>
        public KbQueryDocumentStoreReply DocumentStore(RmContext context, KbQueryDocumentStore param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Write);

                #endregion

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Write);

                var apiResults = new KbQueryDocumentStoreReply()
                {
                    Value = _core.Documents.InsertDocument(
                        transactionReference.Transaction, physicalSchema, param.Document.Content)
                };

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults, 1);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Stores a batch of documents in one round trip. All documents are inserted in a single transaction
        /// (or within the session's explicit transaction, if one is open), so the batch is atomic.
        /// </summary>
        public KbQueryDocumentStoreManyReply DocumentStoreMany(RmContext context, KbQueryDocumentStoreMany param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Write);

                #endregion

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Write);

                var apiResults = new KbQueryDocumentStoreManyReply();
                apiResults.DocumentIds.Capacity = param.Documents.Count;

                foreach (var document in param.Documents)
                {
                    apiResults.DocumentIds.Add(_core.Documents.InsertDocument(
                        transactionReference.Transaction, physicalSchema, document.Content));
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults, apiResults.DocumentIds.Count);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a single document by id.
        /// </summary>
        public KbQueryDocumentGetReply DocumentGet(RmContext context, KbQueryDocumentGet param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Read);

                #endregion

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Read);
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);

                var physicalDocument = _core.Documents.AcquireDocumentVirtual(transactionReference.Transaction, rdb, param.DocumentId, LockOperation.Read);

                var apiResults = new KbQueryDocumentGetReply();
                if (physicalDocument != null)
                {
                    apiResults.Document = new KbDocument
                    {
                        Id = param.DocumentId,
                        Created = physicalDocument.CreatedUTC,
                        Modified = physicalDocument.ModifiedUTC,
                        Content = JsonConvert.SerializeObject(physicalDocument.Elements)
                    };
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults, apiResults.Document == null ? 0 : 1);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Replaces the entire content of an existing document.
        /// </summary>
        public KbQueryDocumentReplaceReply DocumentReplace(RmContext context, KbQueryDocumentReplace param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Write);

                #endregion

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Write);

                _core.Documents.ReplaceDocument(transactionReference.Transaction, physicalSchema, param.DocumentId, param.Document.Content);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryDocumentReplaceReply(), 1);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes documents by id, ignoring ids that do not exist.
        /// </summary>
        public KbQueryDocumentDeleteReply DocumentDelete(RmContext context, KbQueryDocumentDelete param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, param.Schema, SecurityPolicyPermission.Write);

                #endregion

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Delete);

                var apiResults = new KbQueryDocumentDeleteReply
                {
                    DeletedCount = _core.Documents.DeleteExistingDocuments(transactionReference.Transaction, physicalSchema, param.DocumentIds)
                };

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults, apiResults.DeletedCount);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
