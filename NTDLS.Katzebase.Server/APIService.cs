using Newtonsoft.Json;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine;
using NTDLS.Katzebase.Engine.Interactions.Management;
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

                var settings = JsonConvert.DeserializeObject<KatzebaseSettings>(json)
                    ?? throw new Exception("Failed to load settings");

                _settings = settings;

                _core = new EngineCore(settings);

                _messageServer = new RmServer();
                _messageServer.OnException += RmServer_OnException;
                _messageServer.OnConnected += RmServer_OnConnected;
                _messageServer.OnDisconnected += RmServer_OnDisconnected;

                LogManager.Information($"Listening on {_settings.ListenPort}.");

                _messageServer.AddHandler(_core.Documents.APIHandlers);
                _messageServer.AddHandler(_core.Indexes.APIHandlers);
                _messageServer.AddHandler(_core.Query.APIHandlers);
                _messageServer.AddHandler(_core.Schemas.APIHandlers);
                _messageServer.AddHandler(_core.Sessions.APIHandlers);
                _messageServer.AddHandler(_core.Transactions.APIHandlers);
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
                _core.Start();
                _messageServer.Start(_settings.ListenPort);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                LogManager.Information($"Stopping...");
                _messageServer.Stop();
                _core.Stop();
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

            if (_core.Sessions.TryGetProcessByConnection(context.ConnectionId, out var session))
            {
                LogManager.Debug($"Terminated PID: {session.ProcessId}");
                _core.Sessions.CloseByProcessId(session.ProcessId);
            }
        }

        private void RmServer_OnException(RmContext? context, Exception ex, IRmPayload? payload)
        {
            if (ex is KbExceptionBase kbEx)
            {
                switch (kbEx.Severity)
                {
                    case Client.KbConstants.KbLogSeverity.Verbose:
                        LogManager.Verbose(ex);
                        break;
                    case Client.KbConstants.KbLogSeverity.Debug:
                        LogManager.Debug(ex);
                        break;
                    case Client.KbConstants.KbLogSeverity.Information:
                        LogManager.Information(ex);
                        break;
                    case Client.KbConstants.KbLogSeverity.Warning:
                        LogManager.Warning(ex);
                        break;
                    case Client.KbConstants.KbLogSeverity.Error:
                        LogManager.Error(ex);
                        break;
                    case Client.KbConstants.KbLogSeverity.Fatal:
                        LogManager.Fatal(ex);
                        break;
                    default:
                        LogManager.Warning(ex);
                        break;
                }
            }
            else
            {
                LogManager.Warning(ex);
            }
        }
    }
}
