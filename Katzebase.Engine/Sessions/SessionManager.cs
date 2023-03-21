using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katzebase.Engine.Sessions
{
   public class SessionManager
    {
        private Core core;

        private UInt64 nextProcessId = 1;

        public Dictionary<Guid, UInt64> Collection { get; set; }

        public SessionManager(Core core)
        {
            this.core = core;
            Collection = new Dictionary<Guid, ulong>();
        }

        public UInt64 UpsertSessionId(Guid sessionId)
        {
            lock (Collection)
            {
                if (Collection.ContainsKey(sessionId))
                {
                    return Collection[sessionId];
                }
                else
                {
                    UInt64 processId = nextProcessId++;
                    Collection.Add(sessionId, processId);
                    return processId;
                }
            }

        }

    }
}
