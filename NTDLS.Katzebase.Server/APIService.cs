using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Engine;
using NTDLS.Katzebase.Shared;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Server
{
    internal class APIService
    {
        private readonly EngineCore _core;
        private readonly RmServer _messageServer;
        private readonly KatzebaseSettings _settings;

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

                _messageServer = new RmServer();
                _messageServer.OnException += RmServer_OnException;
                _messageServer.OnDisconnected += RmServer_OnDisconnected;

                _core.Log.Verbose($"Listening on {_settings.ListenPort}.");

                _messageServer.AddHandler(_core.Documents.APIHandlers);
                _messageServer.AddHandler(_core.Indexes.APIHandlers);
                _messageServer.AddHandler(_core.Query.APIHandlers);
                _messageServer.AddHandler(_core.Schemas.APIHandlers);
                _messageServer.AddHandler(_core.Sessions.APIHandlers);
                _messageServer.AddHandler(_core.Transactions.APIHandlers);
            }
            catch (Exception ex)
            {
                _core?.Log.Exception(ex);
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
                _core?.Log.Exception(ex);
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
                _core?.Log.Exception(ex);
                throw;
            }
        }

        private void RmServer_OnDisconnected(RmContext context)
        {
            _core.EnsureNotNull();
            _core.Log.Trace($"Disconnected: {context.ConnectionId}");

            var session = _core.Sessions.UpsertConnectionId(context.ConnectionId);
            _core.Sessions.CloseByProcessId(session.ProcessId);
        }

        private void RmServer_OnException(RmContext? context, Exception ex, IRmPayload? payload)
        {
            _core?.Log?.Exception(ex);
        }
    }
}
