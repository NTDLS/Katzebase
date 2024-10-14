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
        /// Returns all documents in a schema with there values.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schemaName"></param>
        /// <param name="rowLimit"></param>
        /// <returns></returns>
        public KbQueryDocumentListReply ListDocuments(RmContext context, KbQueryDocumentList param)
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
        public KbQueryDocumentStoreReply StoreDocument(RmContext context, KbQueryDocumentStore param)
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

                var apiResults = new KbQueryDocumentStoreReply()
                {
                    Value = _core.Documents.InsertDocument(
                        transactionReference.Transaction, param.Schema, param.Document.Content).DocumentId
                };

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults, 1);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
