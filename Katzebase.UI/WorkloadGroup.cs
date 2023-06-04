using Katzebase.Library.Exceptions;

namespace Katzebase.UI
{
    public  class WorkloadGroup
    {
        public bool IsRunning { get; set; }
        public bool IsStopping { get; set; }

        public void Start()
        {
        }

        public void StopAsync()
        {
        }

        public delegate void StoppedEvent(WorkloadGroup sender);
        public event StoppedEvent? OnStopped;

        public delegate void ExceptionEvent(WorkloadGroup sender, KbExceptionBase ex);
        public event ExceptionEvent? OnException;

        public delegate void StatusEvent(WorkloadGroup sender, string text, Color color);
        public event StatusEvent? OnStatus;

        internal void InvokeExceptionEvent(KbExceptionBase ex) => OnException?.Invoke(this, ex);
        internal void InvokeStatusEvent(string text) => OnStatus?.Invoke(this, text, Color.Black);
        internal void InvokeStatusEvent(string text, Color color) => OnStatus?.Invoke(this, text, color);



    }
}
