using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Management
{
    public class WorkloadGroup
    {
        public bool IsRunning { get; set; }
        public bool IsStopping { get; set; }

        public void Start()
        {
            IsRunning = true;
            OnStarting?.Invoke(this);
        }

        public void StopAsync()
        {
            IsStopping = true;
            OnStopping?.Invoke(this);
        }

        public delegate void StartingEvent(WorkloadGroup sender);
        public event StartingEvent? OnStarting;

        public delegate void StoppingEvent(WorkloadGroup sender);
        public event StoppingEvent? OnStopping;

        public delegate void StartedEvent(WorkloadGroup sender);
        public event StartedEvent? OnStarted;

        public delegate void StoppedEvent(WorkloadGroup sender);
        public event StoppedEvent? OnStopped;

        public delegate void ExceptionEvent(WorkloadGroup sender, KbExceptionBase ex);
        public event ExceptionEvent? OnException;

        public delegate void StatusEvent(WorkloadGroup sender, string text, Color color);
        public event StatusEvent? OnStatus;

        internal void InvokeStartedEvent() => OnStarted?.Invoke(this);
        internal void InvokeStoppedEvent() => OnStopped?.Invoke(this);
        internal void InvokeExceptionEvent(KbExceptionBase ex) => OnException?.Invoke(this, ex);
        internal void InvokeStatusEvent(string text) => OnStatus?.Invoke(this, text, Color.Black);
        internal void InvokeStatusEvent(string text, Color color) => OnStatus?.Invoke(this, text, color);
    }
}
