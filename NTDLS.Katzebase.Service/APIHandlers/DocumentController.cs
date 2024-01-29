using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads.Queries;
using NTDLS.Katzebase.Engine;

namespace NTDLS.Katzebase.Service.APIHandlers
{
    public class DocumentController
    {
        private readonly EngineCore _core;
        public DocumentController(EngineCore core)
        {
            _core = core;
        }

        /// <summary>
        /// Lists the documents within a given schema with their values.
        /// </summary>
        /// <param name="schema"></param>
        public KbQueryDocumentListReply List(KbQueryDocumentList param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Documents.APIHandlers.ListDocuments(processId, param.Schema, param.Count);
            }
            catch (Exception ex)
            {
                return new KbQueryDocumentListReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Samples the documents within a given schema with their values.
        /// </summary>
        /// <param name="schema"></param>
        public KbQueryDocumentSampleReply Sample(KbQueryDocumentSample param)
        {
            try
            {
                ulong processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Documents.APIHandlers.DocumentSample(processId, param.Schema, param.Count);
            }
            catch (Exception ex)
            {
                return new KbQueryDocumentSampleReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Lists the documents within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        public KbQueryDocumentCatalogReply Catalog(KbQueryDocumentCatalog param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Documents.APIHandlers.DocumentCatalog(processId, param.Schema);
            }
            catch (Exception ex)
            {
                return new KbQueryDocumentCatalogReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        public KbQueryDocumentStoreReply Store(KbQueryDocumentStore param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Documents.APIHandlers.StoreDocument(processId, param.Schema, param.Document);
            }
            catch (Exception ex)
            {
                return new KbQueryDocumentStoreReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Deletes a single document by its Id.
        /// </summary>
        /// <param name="schema"></param>
        public KbQueryDocumentDeleteByIdReply DeleteById(KbQueryDocumentDeleteById param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Documents.APIHandlers.DeleteDocumentById(processId, param.Schema, param.Id);
            }
            catch (Exception ex)
            {
                return new KbQueryDocumentDeleteByIdReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
