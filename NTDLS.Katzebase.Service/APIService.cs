using Newtonsoft.Json;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads.Queries;
using NTDLS.Katzebase.Engine;
using NTDLS.Katzebase.Service.APIHandlers;
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

        private readonly DocumentController _documentController;
        private readonly IndexesController _indexesController;
        private readonly ProcedureController _procedureController;
        private readonly QueryController _queryController;
        private readonly SchemaController _schemaController;
        private readonly ServerController _serverController;
        private readonly TransactionController _transactionController;

        public APIService()
        {
            string json = File.ReadAllText("appsettings.json");
            var settings = JsonConvert.DeserializeObject<KatzebaseSettings>(json);
            if (settings == null)
            {
                throw new Exception("Failed to load settings");
            }
            _settings = settings;

            _core = new EngineCore(settings);

            _documentController = new DocumentController(_core);
            _indexesController = new IndexesController(_core);
            _procedureController = new ProcedureController(_core);
            _queryController = new QueryController(_core);
            _schemaController = new SchemaController(_core);
            _serverController = new ServerController(_core);
            _transactionController = new TransactionController(_core);

            _messageServer = new MessageServer();
            _messageServer.OnException += MessageServer_OnException;
            _messageServer.OnConnected += MessageServer_OnConnected;
            _messageServer.OnDisconnected += MessageServer_OnDisconnected;
            _messageServer.OnQueryReceived += MessageServer_OnQueryReceived;

            _core.Log.Write($"Listening on {_settings.ListenPort}.");
        }

        public void Start()
        {
            _core.Start();
            _messageServer.Start(_settings.ListenPort);
        }

        public void Stop()
        {
            _core.Log.Write($"Stopping...");
            _messageServer.Stop();
            _core.Stop();
        }

        private void MessageServer_OnConnected(MessageServer server, Guid connectionId, System.Net.Sockets.TcpClient tcpClient)
        {
            Console.WriteLine($"Connected: {connectionId}");
        }

        private void MessageServer_OnException(MessageServer client, Guid connectionId, Exception ex, IFramePayload? payload)
        {
            throw new NotImplementedException();
        }

        private void MessageServer_OnDisconnected(MessageServer server, Guid connectionId)
        {
            Console.WriteLine($"Disconected: {connectionId}");
        }

        private IFramePayloadQueryReply MessageServer_OnQueryReceived(MessageServer server, Guid connectionId, IFramePayloadQuery payload)
        {
            KbUtility.EnsureNotNull(_core);

            if (payload is KbQueryDocumentCatalog queryDocumentCatalog)
            {
                return _documentController.Catalog(queryDocumentCatalog);
            }
            else if (payload is KbQueryDocumentDeleteById queryDocumentDeleteById)
            {
                return _documentController.DeleteById(queryDocumentDeleteById);
            }
            else if (payload is KbQueryDocumentList queryDocumentList)
            {
                return _documentController.List(queryDocumentList);
            }
            else if (payload is KbQueryDocumentSample queryDocumentSample)
            {
                return _documentController.Sample(queryDocumentSample);
            }
            else if (payload is KbQueryDocumentStore queryDocumentStore)
            {
                return _documentController.Store(queryDocumentStore);
            }
            else if (payload is KbQueryIndexCreate queryIndexCreate)
            {
                return _indexesController.Create(queryIndexCreate);
            }
            else if (payload is KbQueryIndexDrop queryIndexDrop)
            {
                return _indexesController.Drop(queryIndexDrop);
            }
            else if (payload is KbQueryIndexExists queryIndexExists)
            {
                return _indexesController.Exists(queryIndexExists);
            }
            else if (payload is KbQueryIndexGet queryIndexGet)
            {
                return _indexesController.Get(queryIndexGet);
            }
            else if (payload is KbQueryIndexList queryIndexList)
            {
                return _indexesController.List(queryIndexList);
            }
            else if (payload is KbQueryIndexRebuild queryIndexRebuild)
            {
                return _indexesController.Rebuild(queryIndexRebuild);
            }
            else if (payload is KbQueryProcedureExecute queryProcedureExecute)
            {
                return _procedureController.ExecuteProcedure(queryProcedureExecute);
            }
            else if (payload is KbQueryQueryExecuteNonQuery queryQueryExecuteNonQuery)
            {
                return _queryController.ExecuteNonQuery(queryQueryExecuteNonQuery);
            }
            else if (payload is KbQueryQueryExecuteQueries queryQueryExecuteQueries)
            {
                return _queryController.ExecuteQueries(queryQueryExecuteQueries);
            }
            else if (payload is KbQueryQueryExecuteQuery queryQueryExecuteQuery)
            {
                return _queryController.ExecuteQuery(queryQueryExecuteQuery);
            }
            else if (payload is KbQueryQueryExplain queryQueryExplain)
            {
                return _queryController.ExplainQuery(queryQueryExplain);
            }
            else if (payload is KbQuerySchemaCreate querySchemaCreate)
            {
                return _schemaController.Create(querySchemaCreate);
            }
            else if (payload is KbQuerySchemaDrop querySchemaDrop)
            {
                return _schemaController.Drop(querySchemaDrop);
            }
            else if (payload is KbQuerySchemaExists querySchemaExists)
            {
                return _schemaController.Exists(querySchemaExists);
            }
            else if (payload is KbQuerySchemaList querySchemaList)
            {
                return _schemaController.List(querySchemaList);
            }
            else if (payload is KbQueryServerCloseSession queryServerCloseSession)
            {
                return _serverController.CloseSession(queryServerCloseSession);
            }
            else if (payload is KbQueryServerTerminateProcess queryServerTerminateProcess)
            {
                return _serverController.TerminateProcess(queryServerTerminateProcess);
            }
            else if (payload is KbQueryTransactionBegin KbQueryTransactionBegin)
            {
                return _transactionController.Begin(KbQueryTransactionBegin);
            }
            else if (payload is KbQueryTransactionCommit queryTransactionCommit)
            {
                return _transactionController.Commit(queryTransactionCommit);
            }
            else if (payload is KbQueryTransactionRollback queryTransactionRollback)
            {
                return _transactionController.Rollback(queryTransactionRollback);
            }

            throw new NotImplementedException();
        }
    }
}
