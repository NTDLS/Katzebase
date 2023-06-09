using Katzebase.PublicLibrary;
using System.Reflection.PortableExecutable;

namespace Katzebase.PrivateLibrary
{
    public static class ThreadPoolHelper
    {
        public static int CalculateThreadCount(int expectedItemCount, double multiplier = 1)
        {
            int maxThreads = (int)Math.Ceiling(Environment.ProcessorCount * 16.0 * multiplier);

            int threads = (int)Math.Ceiling(maxThreads * (expectedItemCount / 10000.0));
            if (threads < 1)
            {
                return 1;
            }
            else if (threads > maxThreads)
            {
                return maxThreads;
            }

            return threads;
        }
    }

    /// <summary>
    /// Creates a pool of threads and a queue of a specified type.
    /// </summary>
    /// <typeparam name="T">The type of item in thw queue.</typeparam>
    /// <typeparam name="P">The type of the parameter that will be passed to the worker thread proc.</typeparam>
    public class ThreadPoolQueue<T, P>
    {
        private readonly List<Thread> _threads = new();
        private int _runningThreadCount = 0;
        private object UserThreadParam { get; set; }
        private UserThreadThread UserThreadProc { get; set; }
        private FixedSizeWaitQueue<T> Queue { get; set; }

        public delegate void UserThreadThread(ThreadPoolQueue<T, P> pool, P? obj);
        public Exception? Exception { get; set; } = null;
        public bool HasException => Exception != null;
        public bool ContinueToProcessQueue { get; private set; } = true;
        public int ThreadCount { get; private set; }
        public int QueueSize { get; private set; }

        public ThreadPoolQueue(UserThreadThread userThreadProc, object userThreadParam, int threadCount, int queueSize)
        {
            Queue = new FixedSizeWaitQueue<T>(queueSize);
            UserThreadParam = userThreadParam;
            UserThreadProc = userThreadProc;
            ThreadCount = threadCount;
            QueueSize = queueSize;
        }

        public void IncrementRunningThreadCount()
        {
            lock (this)
            {
                _runningThreadCount++;
            }
        }

        public void DecrementRunningThreadCount()
        {
            lock (this)
            {
                _runningThreadCount--;
            }
        }

        public static ThreadPoolQueue<T, P> Create(UserThreadThread userThreadProc, object userThreadParam, int threadCount)
        {
            var pool = new ThreadPoolQueue<T, P>(userThreadProc, userThreadParam, threadCount, threadCount * 10);

            return pool;
        }

        public static ThreadPoolQueue<T, P> CreateAndStart(UserThreadThread userThreadProc, object userThreadParam, int threadCount)
        {
            var pool = new ThreadPoolQueue<T, P>(userThreadProc, userThreadParam, threadCount, threadCount * 10);
            pool.Start();
            return pool;
        }

        public void Start()
        {
            Queue = new FixedSizeWaitQueue<T>(QueueSize);

            for (int i = 0; i < ThreadCount; i++)
            {
                var thread = new Thread(PassThroughThreadProc);
                _threads.Add(thread);

                IncrementRunningThreadCount();

                thread.Start(this);
            }
        }

        internal static void PassThroughThreadProc(object? param)
        {
            var pool = param as ThreadPoolQueue<T, P>;
            Utility.EnsureNotNull(pool);

            try
            {
                pool.UserThreadProc(pool, (P)pool.UserThreadParam);
            }
            catch (Exception ex)
            {
                pool.Exception = ex;
            }
            finally
            {
                pool.DecrementRunningThreadCount();
            }
        }

        public void EnqueueWorkItem(T obj)
        {
            Queue.Enqueue(obj);
        }

        public T? DequeueWorkItem()
        {
            return Queue.Dequeue();
        }

        /// <summary>
        /// Call when all items have been queued and we just want to wait on the queue to empty then the threads to finish.
        /// </summary>
        public void WaitForCompletion()
        {
            while (Queue.Count > 0 && HasException == false)
            {
                Thread.Sleep(1);
            }

            ContinueToProcessQueue = false;

            Queue.Stop();

            while (_runningThreadCount > 0)
            {
                Thread.Sleep(1);
            }

            if (Exception != null)
            {
                throw Exception;
            }
        }
    }
}
