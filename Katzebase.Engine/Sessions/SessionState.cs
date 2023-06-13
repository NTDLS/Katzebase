using Katzebase.Engine.KbLib;

namespace Katzebase.Engine.Sessions
{
    public class SessionState
    {
        public enum KbSystemVariable
        {
            TraceWaitTimes,
            MinQueryThreads,
            MaxQueryThreads,
            QueryThreadWeight

        }
        public bool? TraceWaitTimesEnabled { get; set; }
        public int? MinQueryThreads { get; set; } = null;
        public int? MaxQueryThreads { get; set; } = null;
        public double? QueryThreadWeight { get; set; } = null;

        /// <summary>
        /// ProcessId is produced by the server.
        /// </summary>
        public ulong ProcessId { get; private set; }

        /// <summary>
        /// SessionId is produced by the client.
        /// </summary>
        public Guid SessionId { get; set; }

        public List<KbNameValuePair> Variables { get; set; } = new();

        public KbNameValuePair UpsertVariable(string name, string value)
        {
            name = name.ToLowerInvariant();

            var result = Variables.Where(o => o.Name == name).FirstOrDefault();
            if (result != null)
            {
                result.Value = value;
            }
            else
            {
                result = new KbNameValuePair(name, value);
                Variables.Add(result);
            }
            return result;
        }


        public SessionState(ulong processId, Guid sessionId)
        {
            ProcessId = processId;
            SessionId = sessionId;
        }
    }
}
