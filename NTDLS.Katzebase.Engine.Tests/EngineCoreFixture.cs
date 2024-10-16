using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Shared;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Engine.Tests
{
    public class EngineCoreFixture : IDisposable
    {

        private readonly KatzebaseSettings _settings;

        public EngineCore Engine { get; private set; }
        public RmServer MessageServer { get; private set; }

        public EngineCoreFixture()
        {
            try
            {
                Directory.Delete(Constants.ROOT_PATH, true);

                _settings = LoadSettings();

                Engine = new EngineCore(_settings);

                MessageServer = new RmServer();
                MessageServer.OnException += RmServer_OnException;
                MessageServer.OnConnected += RmServer_OnConnected;
                MessageServer.OnDisconnected += RmServer_OnDisconnected;

                LogManager.Information($"Listening on {_settings.ListenPort}.");

                MessageServer.AddHandler(Engine.Documents.APIHandlers);
                MessageServer.AddHandler(Engine.Indexes.APIHandlers);
                MessageServer.AddHandler(Engine.Query.APIHandlers);
                MessageServer.AddHandler(Engine.Schemas.APIHandlers);
                MessageServer.AddHandler(Engine.Sessions.APIHandlers);
                MessageServer.AddHandler(Engine.Transactions.APIHandlers);

                Start();

                using var ephemeral = Engine.Sessions.CreateEphemeralSystemSession();
                ephemeral.Transaction.ExecuteNonQuery(@"Initialization\CreateTestDataSchema.kbs");
                ephemeral.Commit();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw;
            }
        }

        public void Start()
        {
            try
            {
                Engine.Start();
                MessageServer.Start(_settings.ListenPort);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw;
            }
        }

        private KatzebaseSettings LoadSettings()
        {
            var settings = new KatzebaseSettings();

            settings.ListenPort = Constants.LISTEN_PORT;
            settings.DataRootPath = @$"{Constants.ROOT_PATH}\Root";
            settings.TransactionDataPath = @$"{Constants.ROOT_PATH}\Transaction";
            settings.LogDirectory = @$"{Constants.ROOT_PATH}\Logs";

            return settings;
        }

        public void Dispose()
        {
            try
            {
                LogManager.Information($"Stopping network engine.");
                MessageServer.Stop();

                LogManager.Information($"Stopping engine core.");
                Engine.Stop();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw;
            }
        }

        private void RmServer_OnConnected(RmContext context)
        {
            LogManager.Debug($"Connected: {context.ConnectionId}");
        }

        private void RmServer_OnDisconnected(RmContext context)
        {
            LogManager.Debug($"Disconnected: {context.ConnectionId}");

            if (Engine.Sessions.TryGetProcessByConnection(context.ConnectionId, out var processId))
            {
                LogManager.Debug($"Terminated PID: {processId}");
                Engine.Sessions.CloseByProcessId(processId);
            }
        }

        private void RmServer_OnException(RmContext? context, Exception ex, IRmPayload? payload)
        {
            LogManager.Exception(ex);
        }
    }
}
