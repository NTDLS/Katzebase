namespace NTDLS.Semaphore
{
    public interface ICriticalResource
    {
        public bool TryAcquire(int timeout);
        public bool TryAcquire();
        public void Acquire();
        public void Release();
    }
}
