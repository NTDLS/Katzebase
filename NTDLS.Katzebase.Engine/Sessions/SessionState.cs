using NTDLS.Katzebase.Api.Models;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Sessions
{
    /// <summary>
    /// This is the an instance of a single client connection.
    /// </summary>
    internal class SessionState(ulong processId, Guid connectionId, string username, string clientName, List<KbRole> roles, bool isInternalSystemSession)
    {
        /// <summary>
        /// List of roles the user was assigned at login.
        /// </summary>
        public List<KbRole> Roles { get; set; } = roles;

        /// <summary>
        /// The query currently associated with the session.
        /// </summary>
        public Stack<string> QueryTextStack { get; private set; } = new();

        /// <summary>
        /// Settings associated with the session.
        /// </summary>
        public Dictionary<StateSetting, object> Settings { get; private set; } = new();

        /// <summary>
        /// The UTC date/time that the session was created.
        /// </summary>
        public DateTime LoginTime { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// The last UTC date/time that the connection was interacted with.
        /// </summary>
        public DateTime LastCheckInTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ProcessId is produced by the server.
        /// </summary>
        public ulong ProcessId { get; private set; } = processId;

        /// <summary>
        /// SessionId is produced by the client.
        /// </summary>
        public Guid ConnectionId { get; private set; } = connectionId;

        /// <summary>
        /// A user supplied client name to assist in identifying connection sources.
        /// </summary>
        public string ClientName { get; private set; } = clientName;

        /// <summary>
        /// The name of the user logged in to the session.
        /// </summary>
        public string Username { get; private set; } = username;

        /// <summary>
        /// The connection has been disconnected and needs to be cleaned up. We do this because it can be dangerous to perform the locking required to
        /// terminate a connection when it is disconnected so we "try" to terminate the connection then defer to the heartbeat thread usung this flag.
        /// </summary>
        public bool IsExpired { get; set; } = false;

        /// <summary>
        /// Whether this session is a pre-login session. These are used by the engine to access data (like user accounts) to facilitate the login process.
        /// </summary>
        public bool IsInternalSystemSession { get; private set; } = isInternalSystemSession;

        public void UpsertConnectionSetting(StateSetting setting, object value)
        {
            var result = Settings[setting] = value;
        }

        public bool IsConnectionSettingPresent(StateSetting setting)
        {
            return Settings.ContainsKey(setting);
        }

        public T GetConnectionSetting<T>(StateSetting setting, T defaultValue)
        {
            if (Settings.TryGetValue(setting, out var value))
            {
                return (T)value;
            }
            return defaultValue;
        }

        public T? GetConnectionSetting<T>(StateSetting setting)
        {
            if (Settings.TryGetValue(setting, out var value))
            {
                return (T)value;
            }
            return default;
        }

        public void PushCurrentQuery(string statement)
            => QueryTextStack.Push(statement);

        public void PopCurrentQuery()
            => QueryTextStack.Pop();

        public string CurrentQueryText()
        {
            if (QueryTextStack.TryPeek(out var queryString))
            {
                return queryString;
            }
            return string.Empty;
        }
    }
}
