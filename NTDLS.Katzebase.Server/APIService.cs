using Newtonsoft.Json;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Shared;
using NTDLS.ReliableMessaging;
using ProtoBuf;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Server
{
    [ProtoContract]
    public class FString : IStringable
    {
        [ProtoMember(1)]
        private string str;

        public FString()
        {
            this.str = string.Empty; // 初始化為空字符串
        }
        public FString(string s)
        {
            this.str = s;
        }

        public string Value
        {
            get { return str; }
            set { str = value; }
        }

        // Implementing IStringable interface
        public string GetKey()
        {
            return str;
        }

        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(str);
        }

        public IStringable ToLowerInvariant()
        {
            return new FString(str.ToLowerInvariant());
        }

        public T ToT<T>()
        {
            Type targetType = typeof(T);

            if (targetType == typeof(string))
            {
                return (T)(object)str;
            }
            else if (targetType == typeof(double))
            {
                return (T)(object)double.Parse(str);
            }
            else if (targetType == typeof(int))
            {
                return (T)(object)int.Parse(str);
            }
            else
            {
                throw new NotSupportedException($"Type {targetType.Name} is not supported");
            }
        }

        public object ToT(Type targetType)
        {

            if (targetType == typeof(string))
            {
                return (object)str;
            }
            else if (targetType == typeof(double))
            {
                return (object)double.Parse(str);
            }
            else if (targetType == typeof(int))
            {
                return (object)int.Parse(str);
            }
            else
            {
                throw new NotSupportedException($"Type {targetType.Name} is not supported");
            }
        }

        public T ToNullableT<T>() //where T : struct
        {
            Type targetType = typeof(T);

            if (targetType == typeof(double))
            {
                return (T)(object)double.Parse(str);
            }
            else if (targetType == typeof(int))
            {
                return (T)(object)int.Parse(str);
            }
            else
            {
                throw new NotSupportedException($"Type {targetType.Name} is not supported");
            }
        }
    }
    internal class APIService
    {
        private readonly EngineCore<FString> _core;
        private readonly RmServer _messageServer;
        private readonly KatzebaseSettings _settings;

        Func<string, FString> cast = s => new FString(s);

        // parse 函數：這裡可以與 cast 相同，也可以根據具體情況設計不同的解析邏輯
        Func<string, FString> parse = s => new FString(s);

        // compare 函數：用於比較兩個 FString 的值，使用字符串的自然順序比較
        Func<FString, FString, int> compare = (f1, f2) => string.Compare(f1.Value, f2.Value, StringComparison.Ordinal);


        public APIService()
        {
            try
            {
                string json = File.ReadAllText("appsettings.json");

                var settings = JsonConvert.DeserializeObject<KatzebaseSettings>(json)
                    ?? throw new Exception("Failed to load settings");

                _settings = settings;

                _core = new EngineCore<FString>(settings, cast, parse, compare);

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
