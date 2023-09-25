namespace NTDLS.Katzebase.Engine.Heartbeat.Management
{
    internal class HeartbeatManager
    {
        private readonly Core core;
        private readonly Thread threadHandle;
        private bool continueRunning = false;

        public HeartbeatManager(Core core)
        {
            this.core = core;
            threadHandle = new Thread(HearbeatThreadProc);
        }

        public void Start()
        {
            continueRunning = true;
            threadHandle.Start();
        }

        public void Stop()
        {
            continueRunning = false;
        }

        private void HearbeatThreadProc()
        {
            while (continueRunning)
            {
                var expiredSessions = core.Sessions.GetExpiredSessions();
                if (expiredSessions.Any())
                {
                    var expiredProcessIDs = expiredSessions.Select(o => o.ProcessId).ToList();
                    core.Sessions.CloseByProcessIDs(expiredProcessIDs);
                }

                Thread.Sleep(100);
            }
        }
    }
}
