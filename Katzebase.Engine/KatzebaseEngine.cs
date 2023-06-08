using Katzebase.PrivateLibrary;

namespace Katzebase.Engine
{
    public class KatzebaseEngine
    {
        private Core _core;
        public KatzebaseEngine(KatzebaseSettings settings)
        {
            _core = new Core(settings);

        }

        public void Start()
        {
            _core.Start();
        }

        public void Stop()
        {
            _core.Stop();
        }

        public void WriteLog(string message)
        {
            _core.Log.Write(message);
        }
    }
}
