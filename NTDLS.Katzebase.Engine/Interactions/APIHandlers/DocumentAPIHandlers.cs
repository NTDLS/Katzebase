using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers;
using NTDLS.ReliableMessaging;

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
                var result = (KbQueryDocumentSampleReply)StaticSearcherProcessor.SampleSchemaDocuments(_core, transactionReference.Transaction, param.Schema, param.Count);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to execute document sample for process id {session.ProcessId}.", ex);
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
                LogManager.Error($"Failed to execute document list for process id {session.ProcessId}.", ex);
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
                var result = new KbQueryDocumentStoreReply()
                {
                    Value = _core.Documents.InsertDocument(
                        transactionReference.Transaction, param.Schema, param.Document.Content).DocumentId
                };

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 1);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to execute document store for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
