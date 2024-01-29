using Newtonsoft.Json;
using NTDLS.Katzebase.Client.Payloads.Queries;
using NTDLS.Katzebase.Client.Service.Controllers;
using NTDLS.Katzebase.Engine;
using NTDLS.Katzebase.Shared;
using NTDLS.ReliableMessaging;
using NTDLS.StreamFraming.Payloads;
using Topshelf;

namespace NTDLS.Katzebase.Client.Service
{
    public class Program
    {
        private static EngineCore? _core = null;
        public static EngineCore Core
        {
            get
            {
                _core ??= new EngineCore(Configuration);
                return _core;
            }
        }

        private static MessageServer? _messageServer;
        public static MessageServer MessageServer
        {
            get
            {
                _messageServer ??= new MessageServer();
                return _messageServer;
            }
        }

        private static KatzebaseSettings? _settings = null;
        public static KatzebaseSettings Configuration
        {
            get
            {
                if (_settings == null)
                {
                    string json = File.ReadAllText("appsettings.json");
                    _settings = JsonConvert.DeserializeObject<KatzebaseSettings>(json);
                    if (_settings == null)
                    {
                        throw new Exception("Failed to load settings");
                    }
                }

                return _settings;
            }
        }

        public class KatzebaseService
        {
            private SemaphoreSlim _semaphoreToRequestStop;
            private Thread _thread;

            public KatzebaseService()
            {
                _semaphoreToRequestStop = new SemaphoreSlim(0);
                _thread = new Thread(DoWork);
            }

            public void Start()
            {
                _thread.Start();
            }

            public void Stop()
            {
                _semaphoreToRequestStop.Release();
                _thread.Join();
            }

            private void DoWork()
            {
                Core.Start();
                MessageServer.Start(Configuration.ListenPort);

                MessageServer.OnQueryReceived += MessageServer_OnQueryReceived;

                Core.Log.Write($"Listening on {Configuration.ListenPort}.");

                while (true)
                {
                    if (_semaphoreToRequestStop.Wait(500))
                    {
                        Core.Log.Write($"Stopping...");
                        MessageServer.Stop();
                        Core.Stop();
                        break;
                    }
                }
            }

            private IFramePayloadQueryReply MessageServer_OnQueryReceived(MessageServer server, Guid connectionId, IFramePayloadQuery payload)
            {
                if (payload is KbQueryDocumentCatalog queryDocumentCatalog)
                {
                    return DocumentController.Catalog(queryDocumentCatalog);
                }
                else if (payload is KbQueryDocumentDeleteById queryDocumentDeleteById)
                {
                    return DocumentController.DeleteById(queryDocumentDeleteById);
                }
                else if (payload is KbQueryDocumentList queryDocumentList)
                {
                    return DocumentController.List(queryDocumentList);
                }
                else if (payload is KbQueryDocumentSample queryDocumentSample)
                {
                    return DocumentController.Sample(queryDocumentSample);
                }
                else if (payload is KbQueryDocumentStore queryDocumentStore)
                {
                    return DocumentController.Store(queryDocumentStore);
                }
                else if (payload is KbQueryIndexCreate queryIndexCreate)
                {
                    return IndexesController.Create(queryIndexCreate);
                }
                else if (payload is KbQueryIndexDrop queryIndexDrop)
                {
                    return IndexesController.Drop(queryIndexDrop);
                }
                else if (payload is KbQueryIndexExists queryIndexExists)
                {
                    return IndexesController.Exists(queryIndexExists);
                }
                else if (payload is KbQueryIndexGet queryIndexGet)
                {
                    return IndexesController.Get(queryIndexGet);
                }
                else if (payload is KbQueryIndexList queryIndexList)
                {
                    return IndexesController.List(queryIndexList);
                }
                else if (payload is KbQueryIndexRebuild queryIndexRebuild)
                {
                    return IndexesController.Rebuild(queryIndexRebuild);
                }
                else if (payload is KbQueryProcedureExecute queryProcedureExecute)
                {
                    return ProcedureController.ExecuteProcedure(queryProcedureExecute);
                }
                else if (payload is KbQueryQueryExecuteNonQuery queryQueryExecuteNonQuery)
                {
                    return QueryController.ExecuteNonQuery(queryQueryExecuteNonQuery);
                }
                else if (payload is KbQueryQueryExecuteQueries queryQueryExecuteQueries)
                {
                    return QueryController.ExecuteQueries(queryQueryExecuteQueries);
                }
                else if (payload is KbQueryQueryExecuteQuery queryQueryExecuteQuery)
                {
                    return QueryController.ExecuteQuery(queryQueryExecuteQuery);
                }
                else if (payload is KbQueryQueryExplain queryQueryExplain)
                {
                    return QueryController.ExplainQuery(queryQueryExplain);
                }
                else if (payload is KbQuerySchemaCreate querySchemaCreate)
                {
                    return SchemaController.Create(querySchemaCreate);
                }
                else if (payload is KbQuerySchemaDrop querySchemaDrop)
                {
                    return SchemaController.Drop(querySchemaDrop);
                }
                else if (payload is KbQuerySchemaExists querySchemaExists)
                {
                    return SchemaController.Exists(querySchemaExists);
                }
                else if (payload is KbQuerySchemaList querySchemaList)
                {
                    return SchemaController.List(querySchemaList);
                }
                else if (payload is KbQueryServerCloseSession queryServerCloseSession)
                {
                    return ServerController.CloseSession(queryServerCloseSession);
                }
                else if (payload is KbQueryServerTerminateProcess queryServerTerminateProcess)
                {
                    return ServerController.TerminateProcess(queryServerTerminateProcess);
                }
                else if (payload is KbQueryTransactionBegin KbQueryTransactionBegin)
                {
                    return TransactionController.Begin(KbQueryTransactionBegin);
                }
                else if (payload is KbQueryTransactionCommit queryTransactionCommit)
                {
                    return TransactionController.Commit(queryTransactionCommit);
                }
                else if (payload is KbQueryTransactionRollback queryTransactionRollback)
                {
                    return TransactionController.Rollback(queryTransactionRollback);
                }

                throw new NotImplementedException();
            }
        }

        public static void Main()
        {
            HostFactory.Run(x =>
            {
                x.StartAutomatically(); // Start the service automatically

                x.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1); // restart the service after 1 minute
                });

                x.Service<KatzebaseService>(s =>
                {
                    s.ConstructUsing(hostSettings => new KatzebaseService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Katzebase document-based database services.");
                x.SetDisplayName("Katzebase Service");
                x.SetServiceName("Katzebase");
            });
        }
    }
}
