using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTDLS.Katzebase.Engine.Health
{
    internal class HeartbeatManager
    {
        private readonly EngineCore _core;
        private readonly Thread _threadHandle;
        private bool _continueRunning = false;

        public HeartbeatManager(EngineCore core)
        {
            _core = core;
            _threadHandle = new Thread(HeartbeatThreadProc);
        }

        public void Start()
        {
            _continueRunning = true;
            _threadHandle.Start();
        }

        public void Stop()
        {
            _continueRunning = false;
        }

        private void HeartbeatThreadProc()
        {
            while (_continueRunning)
            {
                /*
                var expiredSessions = _core.Sessions.GetExpiredSessions();
                if (expiredSessions.Any())
                {
                    var processIds = expiredSessions.Select(o => o.ProcessId).ToList();
                    foreach (var processId in processIds)
                    {
                        _core.Sessions.CloseByProcessId(processId);
                    }
                }

                Thread.Sleep(100);
                */
            }
        }
    }

}
