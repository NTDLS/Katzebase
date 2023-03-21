using System;
using System.Collections.Generic;

namespace Katzebase.Engine.Sessions
{
    public class SessionManager
    {
        private Core core;

        private ulong nextProcessId = 1;

        public Dictionary<Guid, ulong> Collection { get; set; }

        public SessionManager(Core core)
        {
            this.core = core;
            Collection = new Dictionary<Guid, ulong>();
        }

        public ulong UpsertSessionId(Guid sessionId)
        {
            lock (Collection)
            {
                if (Collection.ContainsKey(sessionId))
                {
                    return Collection[sessionId];
                }
                else
                {
                    ulong processId = nextProcessId++;
                    Collection.Add(sessionId, processId);
                    return processId;
                }
            }

        }

    }
}
