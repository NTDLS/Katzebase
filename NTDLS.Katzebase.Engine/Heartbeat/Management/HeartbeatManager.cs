namespace NTDLS.Katzebase.Engine.Heartbeat.Management
{
    internal class HeartbeatManager
    {
        private readonly Core _core;
        private readonly Thread _threadHandle;
        private bool _continueRunning = false;

        public HeartbeatManager(Core core)
        {
            _core = core;
            _threadHandle = new Thread(HearbeatThreadProc);
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

        private void HearbeatThreadProc()
        {
            while (_continueRunning)
            {
                var expiredSessions = _core.Sessions.GetExpiredSessions();
                if (expiredSessions.Any())
                {
                    var expiredProcessIDs = expiredSessions.Select(o => o.ProcessId).ToList();
                    _core.Sessions.CloseByProcessIDs(expiredProcessIDs);
                }

                Thread.Sleep(100);
            }
        }
    }
}
