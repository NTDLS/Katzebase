using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Shared;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Engine.Tests
{
    public class EngineCoreSingleton
    {
        private static int _referenceCount = 0;

        private static readonly object _lock = new object();

        private static KatzebaseSettings? _settings;
        private static EngineCore? _engine;
        private static RmServer? _messageServer;

        public static EngineCore GetSingleInstance()
        {
            _referenceCount++;

            if (_engine == null)
            {
                lock (_lock)
                {
                    if (_engine == null)
                    {
                        _engine = CreateNewInstance();
                    }
                }
            }

            return _engine;
        }

        private static EngineCore CreateNewInstance()
        {
            //Manually Delete the root directory to have the test data generated.
            bool rootDirectoryFreshlyCreated = Directory.Exists(Constants.ROOT_PATH);

            _settings = new KatzebaseSettings();
            _settings.ListenPort = Constants.LISTEN_PORT;
            _settings.DataRootPath = @$"{Constants.ROOT_PATH}\Root";
            _settings.TransactionDataPath = @$"{Constants.ROOT_PATH}\Transaction";
            _settings.LogDirectory = @$"{Constants.ROOT_PATH}\Logs";

            _messageServer = new RmServer();
            _messageServer.OnException += RmServer_OnException;
            _messageServer.OnConnected += RmServer_OnConnected;
            _messageServer.OnDisconnected += RmServer_OnDisconnected;

            LogManager.Information($"Listening on {_settings.ListenPort}.");

            var engine = new EngineCore(_settings);
            _messageServer.AddHandler(engine.Documents.APIHandlers);
            _messageServer.AddHandler(engine.Indexes.APIHandlers);
            _messageServer.AddHandler(engine.Query.APIHandlers);
            _messageServer.AddHandler(engine.Schemas.APIHandlers);
            _messageServer.AddHandler(engine.Sessions.APIHandlers);
            _messageServer.AddHandler(engine.Transactions.APIHandlers);

            engine.Start();
            _messageServer.Start(_settings.ListenPort);

            StaticGenerateTestData.GenerateTestData(rootDirectoryFreshlyCreated);

            return engine;
        }

        public static void Dereference()
        {
            _referenceCount--;

            if (_referenceCount == 0)
            {
                lock (_lock)
                {
                    _engine?.Stop();
                    _engine = null;
                }
            }
        }

        private static void RmServer_OnConnected(RmContext context)
        {
            LogManager.Debug($"Connected: {context.ConnectionId}");
        }

        private static void RmServer_OnDisconnected(RmContext context)
        {
            if (_engine == null)
            {
                return;
            }

            LogManager.Debug($"Disconnected: {context.ConnectionId}");

            if (_engine.Sessions.TryGetProcessByConnection(context.ConnectionId, out var processId))
            {
                LogManager.Debug($"Terminated PID: {processId}");
                _engine.Sessions.CloseByProcessId(processId);
            }
        }

        private static void RmServer_OnException(RmContext? context, Exception ex, IRmPayload? payload)
        {
            LogManager.Exception(ex);
        }
    }
}
