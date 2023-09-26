using NTDLS.Katzebase.Engine.Library;

namespace NTDLS.Katzebase.Engine.Sessions
{
    public class SessionState
    {
        public enum KbConnectionSetting
        {
            TraceWaitTimes,
            MinQueryThreads,
            MaxQueryThreads,
            QueryThreadWeight
        }

        /// <summary>
        /// Settings associated with the connection.
        /// </summary>
        public List<KbNameValuePair<KbConnectionSetting, double>> Variables { get; private set; } = new();

        //public bool? TraceWaitTimesEnabled { get; set; }
        //public int? MinQueryThreads { get; set; } = null;
        //public int? MaxQueryThreads { get; set; } = null;
        //public double? QueryThreadWeight { get; set; } = null;

        /// <summary>
        /// The UTC date/time that the session was created.
        /// </summary>
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The last UTC date/time that the connection was interacted with.
        /// </summary>
        public DateTime LastCheckinTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ProcessId is produced by the server.
        /// </summary>
        public ulong ProcessId { get; private set; }

        /// <summary>
        /// SessionId is produced by the client.
        /// </summary>
        public Guid SessionId { get; set; }


        public KbNameValuePair<KbConnectionSetting, double> UpsertConnectionSetting(KbConnectionSetting name, double value)
        {
            var result = Variables.Where(o => o.Name == name).FirstOrDefault();
            if (result != null)
            {
                result.Value = value;
            }
            else
            {
                result = new(name, value);
                Variables.Add(result);
            }
            return result;
        }

        public bool IsConnectionSettingSet(KbConnectionSetting name)
        {
            var result = Variables.Where(o => o.Name == name).FirstOrDefault();
            return result != null;
        }

        public double? GetConnectionSetting(KbConnectionSetting name)
        {
            var result = Variables.Where(o => o.Name == name).FirstOrDefault();
            if (result != null)
            {
                return result.Value;
            }
            return null;
        }

        public SessionState(ulong processId, Guid sessionId)
        {
            ProcessId = processId;
            SessionId = sessionId;
        }
    }
}
