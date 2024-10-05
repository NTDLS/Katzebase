using Newtonsoft.Json;
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
                _settings = LoadSettings("appsettings.json");

                _core = new EngineCore(_settings);

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

        private KatzebaseSettings LoadSettings(string fileName)
        {
            var defaultSettings = new KatzebaseSettings();

            //File doesn't exist? Create it and return the default configuration.
            if (File.Exists(fileName) == false)
            {
                try
                {
                    File.WriteAllText(fileName, JsonConvert.SerializeObject(defaultSettings));
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to create default settings file: [{fileName}], Error: {ex.Message}.");
                }
                return defaultSettings;
            }

            var settingsJson = File.ReadAllText(fileName).Trim();

            //File was empty? Create a new one and return the default configuration.
            if (string.IsNullOrEmpty(settingsJson))
            {
                try
                {
                    File.WriteAllText(fileName, JsonConvert.SerializeObject(defaultSettings));
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to create default settings file: [{fileName}], Error: {ex.Message}.");
                }
                return defaultSettings;
            }

            try
            {
                var loadedSettings = JsonConvert.DeserializeObject<KatzebaseSettings>(settingsJson);

                //Failed to deserialization? Return the default configuration.
                if (loadedSettings == null)
                {
                    LogManager.Error($"Failed to deserialize settings file: [{fileName}], Error: deserialization resulted in null.");
                    return defaultSettings;
                }
                return loadedSettings;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to deserialize settings file: [{fileName}], Error: {ex.Message}.");
            }

            //All else failed, return the default configuration.
            LogManager.Error($"Failed to load settings file: [{fileName}], using default configuration.");
            return defaultSettings;
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
                LogManager.Information($"Stopping network engine.");
                _messageServer.Stop();

                LogManager.Information($"Stopping engine core.");
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

            if (_core.Sessions.TryGetProcessByConnection(context.ConnectionId, out var processId))
            {
                LogManager.Debug($"Terminated PID: {processId}");
                _core.Sessions.CloseByProcessId(processId);
            }
        }

        private void RmServer_OnException(RmContext? context, Exception ex, IRmPayload? payload)
        {
            LogManager.Exception(ex);
        }
    }
}
