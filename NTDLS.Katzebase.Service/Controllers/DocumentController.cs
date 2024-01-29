using NTDLS.Katzebase.Client.Payloads.Queries;

namespace NTDLS.Katzebase.Client.Service.Controllers
{
    public static class DocumentController
    {
        /// <summary>
        /// Lists the documents within a given schema with their values.
        /// </summary>
        /// <param name="schema"></param>
        public static KbQueryDocumentListReply List(KbQueryDocumentList param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Documents.APIHandlers.ListDocuments(processId, param.Schema, param.Count);
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
        public static KbQueryDocumentSampleReply Sample(KbQueryDocumentSample param)
        {
            try
            {
                ulong processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Documents.APIHandlers.DocumentSample(processId, param.Schema, param.Count);
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
        public static KbQueryDocumentCatalogReply Catalog(KbQueryDocumentCatalog param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Documents.APIHandlers.DocumentCatalog(processId, param.Schema);
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

        public static KbQueryDocumentStoreReply Store(KbQueryDocumentStore param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Documents.APIHandlers.StoreDocument(processId, param.Schema, param.Document);
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
        public static KbQueryDocumentDeleteByIdReply DeleteById(KbQueryDocumentDeleteById param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Documents.APIHandlers.DeleteDocumentById(processId, param.Schema, param.Id);
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
