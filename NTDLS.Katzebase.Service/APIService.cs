using Newtonsoft.Json;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine;
using NTDLS.Katzebase.Shared;
using NTDLS.ReliableMessaging;
using NTDLS.StreamFraming.Payloads;

namespace NTDLS.Katzebase.Service
{
    internal class APIService
    {
        private readonly EngineCore _core;
        private readonly MessageServer _messageServer;
        private readonly KatzebaseSettings _settings;

        private delegate IFramePayloadQueryReply APIMessageHandler(Guid connectionId, ulong processId, IFramePayloadQuery payload);

        private readonly Dictionary<Type, APIMessageHandler> _handlerMapings;

        public APIService()
        {
            try
            {
                string json = File.ReadAllText("appsettings.json");
                var settings = JsonConvert.DeserializeObject<KatzebaseSettings>(json);
                if (settings == null)
                {
                    throw new Exception("Failed to load settings");
                }
                _settings = settings;

                _core = new EngineCore(settings);

                _messageServer = new MessageServer();
                _messageServer.OnException += MessageServer_OnException;
                _messageServer.OnDisconnected += MessageServer_OnDisconnected;
                _messageServer.OnQueryReceived += MessageServer_OnQueryReceived;

                _core.Log.Verbose($"Listening on {_settings.ListenPort}.");

                _handlerMapings = new()
                {
                    { typeof(KbQueryDocumentCatalog), (connectionId, processId, payload) => _core.Documents.APIHandlers.DocumentCatalog(processId, (KbQueryDocumentCatalog)payload) },
                    { typeof(KbQueryDocumentDeleteById), (connectionId, processId, payload) => _core.Documents.APIHandlers.DeleteDocumentById(processId, (KbQueryDocumentDeleteById)payload) },
                    { typeof(KbQueryDocumentList), (connectionId, processId, payload) => _core.Documents.APIHandlers.ListDocuments(processId, (KbQueryDocumentList)payload) },
                    { typeof(KbQueryDocumentSample), (connectionId, processId, payload) => _core.Documents.APIHandlers.DocumentSample(processId, (KbQueryDocumentSample)payload) },
                    { typeof(KbQueryDocumentStore), (connectionId, processId, payload) => _core.Documents.APIHandlers.StoreDocument(processId, (KbQueryDocumentStore)payload) },
                    { typeof(KbQueryIndexCreate), (connectionId, processId, payload) => _core.Indexes.APIHandlers.CreateIndex(processId, (KbQueryIndexCreate)payload) },
                    { typeof(KbQueryIndexDrop), (connectionId, processId, payload) => _core.Indexes.APIHandlers.DropIndex(processId, (KbQueryIndexDrop)payload) },
                    { typeof(KbQueryIndexExists), (connectionId, processId, payload) => _core.Indexes.APIHandlers.DoesIndexExist(processId, (KbQueryIndexExists)payload) },
                    { typeof(KbQueryIndexGet), (connectionId, processId, payload) => _core.Indexes.APIHandlers.Get(processId, (KbQueryIndexGet)payload) },
                    { typeof(KbQueryIndexList), (connectionId, processId, payload) => _core.Indexes.APIHandlers.ListIndexes(processId, (KbQueryIndexList)payload) },
                    { typeof(KbQueryIndexRebuild), (connectionId, processId, payload) => _core.Indexes.APIHandlers.RebuildIndex(processId, (KbQueryIndexRebuild)payload) },
                    { typeof(KbQueryProcedureExecute), (connectionId, processId, payload) => _core.Query.APIHandlers.ExecuteStatementProcedure(processId, (KbQueryProcedureExecute)payload) },
                    { typeof(KbQueryQueryExecuteNonQuery), (connectionId, processId, payload) => _core.Query.APIHandlers.ExecuteStatementNonQuery(processId, (KbQueryQueryExecuteNonQuery)payload) },
                    { typeof(KbQueryQueryExecuteQueries), (connectionId, processId, payload) => _core.Query.APIHandlers.ExecuteStatementQueries(processId, (KbQueryQueryExecuteQueries)payload) },
                    { typeof(KbQueryQueryExecuteQuery), (connectionId, processId, payload) => _core.Query.APIHandlers.ExecuteStatementQuery(processId, (KbQueryQueryExecuteQuery)payload) },
                    { typeof(KbQueryQueryExplain), (connectionId, processId, payload) => _core.Query.APIHandlers.ExecuteStatementExplain(processId, (KbQueryQueryExplain)payload) },
                    { typeof(KbQuerySchemaCreate), (connectionId, processId, payload) => _core.Schemas.APIHandlers.CreateSchema(processId, (KbQuerySchemaCreate)payload) },
                    { typeof(KbQuerySchemaDrop), (connectionId, processId, payload) => _core.Schemas.APIHandlers.DropSchema(processId, (KbQuerySchemaDrop)payload) },
                    { typeof(KbQuerySchemaExists), (connectionId, processId, payload) => _core.Schemas.APIHandlers.DoesSchemaExist(processId, (KbQuerySchemaExists)payload) },
                    { typeof(KbQuerySchemaList), (connectionId, processId, payload) => _core.Schemas.APIHandlers.ListSchemas(processId, (KbQuerySchemaList)payload) },
                    { typeof(KbQueryServerStartSession), (connectionId, processId, payload) => _core.Sessions.APIHandlers.StartSession(connectionId) },
                    { typeof(KbQueryServerCloseSession), (connectionId, processId, payload) => _core.Sessions.APIHandlers.CloseSession(processId) },
                    { typeof(KbQueryServerTerminateProcess), (connectionId, processId, payload) => _core.Sessions.APIHandlers.TerminateSession(processId, (KbQueryServerTerminateProcess)payload) },
                    { typeof(KbQueryTransactionBegin), (connectionId, processId, payload) => _core.Transactions.APIHandlers.Begin(processId) },
                    { typeof(KbQueryTransactionCommit), (connectionId, processId, payload) => _core.Transactions.APIHandlers.Commit(processId) },
                    { typeof(KbQueryTransactionRollback), (connectionId, processId, payload) => _core.Transactions.APIHandlers.Rollback(processId)}
                };
            }
            catch (Exception ex)
            {
                _core?.Log.Write(ex);
                throw;
            }
        }

        public void Start()
        {
            try
            {
                _core.Start();
                _messageServer.Start(_settings.ListenPort);
            }
            catch (Exception ex)
            {
                _core?.Log.Write(ex);
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                _core.Log.Verbose($"Stopping...");
                _messageServer.Stop();
                _core.Stop();
            }
            catch (Exception ex)
            {
                _core?.Log.Write(ex);
                throw;
            }
        }

        private void MessageServer_OnDisconnected(MessageServer server, Guid connectionId)
        {
            _core?.Log.Trace($"Disonnected: {connectionId}");
            KbUtility.EnsureNotNull(_core);

            var processId = _core.Sessions.UpsertConnectionId(connectionId);
            _core.Sessions.CloseByProcessId(processId);
        }

        private void MessageServer_OnException(MessageServer client, Guid connectionId, Exception ex, IFramePayload? payload)
        {
            throw new NotImplementedException();
        }

        private IFramePayloadQueryReply MessageServer_OnQueryReceived(MessageServer server, Guid connectionId, IFramePayloadQuery payload)
        {
            KbUtility.EnsureNotNull(_core);

            var processId = _core.Sessions.UpsertConnectionId(connectionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{payload.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);

            if (_handlerMapings.TryGetValue(payload.GetType(), out var handler))
            {
                return handler(connectionId, processId, payload);
            }

            throw new NotImplementedException();
        }
    }
}
