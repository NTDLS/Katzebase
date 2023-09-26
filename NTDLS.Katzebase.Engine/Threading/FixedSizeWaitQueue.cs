using System.Collections.Concurrent;

namespace NTDLS.Katzebase.Engine.Threading
{
    public class FixedSizeWaitQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new();
        private bool _keepRunning = true;
        private readonly AutoResetEvent _queued = new(false);

        public int MaxSize { get; private set; }
        public int Count => _queue.Count;
        public ulong CumulativeQueuedCount { get; private set; } = 0;
        public ulong CumulativeDequeuedCount { get; private set; } = 0;

        public FixedSizeWaitQueue(int maxSize)
        {
            MaxSize = maxSize;
        }

        public void Stop()
        {
            _keepRunning = false;
        }

        public void Enqueue(T obj)
        {
            while (_keepRunning && _queue.Count >= MaxSize)
            {
                Thread.Sleep(1);
            }

            _queue.Enqueue(obj);
            CumulativeQueuedCount++;
            _queued.Set();
        }

        public T? Dequeue()
        {
            T? result = default;

            while (_keepRunning && _queue.TryDequeue(out result) == false)
            {
                _queued.WaitOne(1);
            }

            CumulativeDequeuedCount++;

            return result;
        }
    }
}
