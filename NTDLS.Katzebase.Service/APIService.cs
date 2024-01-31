using Newtonsoft.Json;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads.Queries;
using NTDLS.Katzebase.Engine;
using NTDLS.Katzebase.Shared;
using NTDLS.ReliableMessaging;
using NTDLS.StreamFraming.Payloads;
using System.Diagnostics;

namespace NTDLS.Katzebase.Service
{
    internal class APIService
    {
        private readonly EngineCore _core;
        private readonly MessageServer _messageServer;
        private readonly KatzebaseSettings _settings;

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

        private void MessageServer_OnDisconnected(MessageServer server, Guid connectionId)
        {
            Console.WriteLine($"Disconected: {connectionId}");

            KbUtility.EnsureNotNull(_core);

            var processId = _core.Sessions.UpsertSessionId(connectionId);
            _core.Sessions.CloseByProcessId(processId);
        }

        private void MessageServer_OnException(MessageServer client, Guid connectionId, Exception ex, IFramePayload? payload)
        {
            throw new NotImplementedException();
        }

        private IFramePayloadQueryReply MessageServer_OnQueryReceived(MessageServer server, Guid connectionId, IFramePayloadQuery payload)
        {
            KbUtility.EnsureNotNull(_core);

            var processId = _core.Sessions.UpsertSessionId(connectionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{payload.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);

            if (payload is KbQueryDocumentCatalog queryDocumentCatalog)
            {
                return _core.Documents.APIHandlers.DocumentCatalog(processId, queryDocumentCatalog.Schema);
            }
            else if (payload is KbQueryDocumentDeleteById queryDocumentDeleteById)
            {
                return _core.Documents.APIHandlers.DeleteDocumentById(processId, queryDocumentDeleteById.Schema, queryDocumentDeleteById.Id);
            }
            else if (payload is KbQueryDocumentList queryDocumentList)
            {
                return _core.Documents.APIHandlers.ListDocuments(processId, queryDocumentList.Schema, queryDocumentList.Count);
            }
            else if (payload is KbQueryDocumentSample queryDocumentSample)
            {
                return _core.Documents.APIHandlers.DocumentSample(processId, queryDocumentSample.Schema, queryDocumentSample.Count);
            }
            else if (payload is KbQueryDocumentStore queryDocumentStore)
            {
                return _core.Documents.APIHandlers.StoreDocument(processId, queryDocumentStore.Schema, queryDocumentStore.Document);
            }
            else if (payload is KbQueryIndexCreate queryIndexCreate)
            {
                return _core.Indexes.APIHandlers.CreateIndex(processId, queryIndexCreate.Schema, queryIndexCreate.Index);
            }
            else if (payload is KbQueryIndexDrop queryIndexDrop)
            {
                return _core.Indexes.APIHandlers.DropIndex(processId, queryIndexDrop.Schema, queryIndexDrop.IndexName);
            }
            else if (payload is KbQueryIndexExists queryIndexExists)
            {
                return _core.Indexes.APIHandlers.DoesIndexExist(processId, queryIndexExists.Schema, queryIndexExists.IndexName);
            }
            else if (payload is KbQueryIndexGet queryIndexGet)
            {
                return _core.Indexes.APIHandlers.Get(processId, queryIndexGet.Schema, queryIndexGet.IndexName);
            }
            else if (payload is KbQueryIndexList queryIndexList)
            {
                return _core.Indexes.APIHandlers.ListIndexes(processId, queryIndexList.Schema);
            }
            else if (payload is KbQueryIndexRebuild queryIndexRebuild)
            {
                return _core.Indexes.APIHandlers.RebuildIndex(processId, queryIndexRebuild.Schema, queryIndexRebuild.IndexName, queryIndexRebuild.NewPartitionCount);
            }
            else if (payload is KbQueryProcedureExecute queryProcedureExecute)
            {
                return _core.Query.APIHandlers.ExecuteStatementProcedure(processId, queryProcedureExecute.Procedure);
            }
            else if (payload is KbQueryQueryExecuteNonQuery queryQueryExecuteNonQuery)
            {
                return _core.Query.APIHandlers.ExecuteStatementNonQuery(processId, queryQueryExecuteNonQuery.Statement);
            }
            else if (payload is KbQueryQueryExecuteQueries queryQueryExecuteQueries)
            {
                return _core.Query.APIHandlers.ExecuteStatementQueries(processId, queryQueryExecuteQueries.Statements);
            }
            else if (payload is KbQueryQueryExecuteQuery queryQueryExecuteQuery)
            {
                return _core.Query.APIHandlers.ExecuteStatementQuery(processId, queryQueryExecuteQuery.Statement);
            }
            else if (payload is KbQueryQueryExplain queryQueryExplain)
            {
                return _core.Query.APIHandlers.ExecuteStatementExplain(processId, queryQueryExplain.Statement);
            }
            else if (payload is KbQuerySchemaCreate querySchemaCreate)
            {
                return _core.Schemas.APIHandlers.CreateSchema(processId, querySchemaCreate.Schema, querySchemaCreate.PageSize);
            }
            else if (payload is KbQuerySchemaDrop querySchemaDrop)
            {
                return _core.Schemas.APIHandlers.DropSchema(processId, querySchemaDrop.Schema);
            }
            else if (payload is KbQuerySchemaExists querySchemaExists)
            {
                return _core.Schemas.APIHandlers.DoesSchemaExist(processId, querySchemaExists.Schema);
            }
            else if (payload is KbQuerySchemaList querySchemaList)
            {
                return _core.Schemas.APIHandlers.ListSchemas(processId, querySchemaList.Schema);
            }
            else if (payload is KbQueryServerStartSession queryServerStartSession)
            {
                var result = new KbQueryServerStartSessionReply
                {
                    ProcessId = processId,
                    SessionId = connectionId,
                    ServerTimeUTC = DateTime.UtcNow,
                    Success = true
                };

                return result;
            }
            else if (payload is KbQueryServerCloseSession queryServerCloseSession)
            {
                _core.Sessions.CloseByProcessId(processId);

                var result = new KbQueryServerCloseSessionReply
                {
                    Success = true
                };

                return result;
            }
            else if (payload is KbQueryServerTerminateProcess queryServerTerminateProcess)
            {
                _core.Sessions.CloseByProcessId(queryServerTerminateProcess.ReferencedProcessId);

                var result = new KbQueryServerTerminateProcessReply
                {
                    Success = true
                };

                return result;
            }
            else if (payload is KbQueryTransactionBegin KbQueryTransactionBegin)
            {
                _core.Transactions.APIHandlers.Begin(processId);
                return new KbQueryTransactionCommitReply { Success = true };
            }
            else if (payload is KbQueryTransactionCommit queryTransactionCommit)
            {
                _core.Transactions.APIHandlers.Commit(processId);
                return new KbQueryTransactionCommitReply { Success = true };
            }
            else if (payload is KbQueryTransactionRollback queryTransactionRollback)
            {
                _core.Transactions.APIHandlers.Rollback(processId);
                return new KbQueryTransactionRollbackReply { Success = true };
            }

            throw new NotImplementedException();
        }
    }
}
