using System.Collections.Concurrent;

namespace Katzebase.Engine.Threading
{
    public class FixedSizeWaitQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        public int MaxSize { get; private set; }
        private bool _keepRunning = true;
        private AutoResetEvent _queued = new(false);
        public int Count => _queue.Count;

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
            _queued.Set();
        }

        public T? Dequeue()
        {
            T? result = default(T);

            while (_keepRunning && _queue.TryDequeue(out result) == false)
            {
                _queued.WaitOne(1);
            }

            return result;
        }
    }
}
