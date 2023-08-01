using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Sessions;
using Katzebase.PublicLibrary;

namespace Katzebase.Engine.Threading
{
    public static class ThreadPoolHelper
    {
        internal static int CalculateThreadCount(Core core, Transaction transaction, int expectedItemCount, double multiplier = 1)
        {
            var session = core.Sessions.ByProcessId(transaction.ProcessId);

            if (session.IsConnectionSettingSet(SessionState.KbConnectionSetting.QueryThreadWeight))
            {
                multiplier = session.GetConnectionSetting(SessionState.KbConnectionSetting.QueryThreadWeight) ?? multiplier;
            }

            int maxThreads = (int)Math.Ceiling(Environment.ProcessorCount * 16.0 * multiplier);
            if (session.IsConnectionSettingSet(SessionState.KbConnectionSetting.MaxQueryThreads))
            {
                maxThreads = (int)(session.GetConnectionSetting(SessionState.KbConnectionSetting.MaxQueryThreads) ?? maxThreads);
            }

            if (maxThreads > core.Settings.MaxQueryThreads)
            {
                maxThreads = core.Settings.MaxQueryThreads;
            }

            int minThreads = core.Settings.MinQueryThreads;
            if (session.IsConnectionSettingSet(SessionState.KbConnectionSetting.MinQueryThreads))
            {
                minThreads = (int)(session.GetConnectionSetting(SessionState.KbConnectionSetting.MinQueryThreads) ?? minThreads);
            }

            int threads = (int)Math.Ceiling((expectedItemCount / 10000.0));
            if (threads < minThreads)
            {
                threads = minThreads;
            }
            else if (threads > maxThreads)
            {
                threads = maxThreads;
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
        private class PassThroughThreadParam<TPT, TPP>
        {
            public ThreadPoolQueue<TPT, TPP> Pool { get; set; }
            public int ThreadNumber { get; set; }

            public PassThroughThreadParam(ThreadPoolQueue<TPT, TPP> pool, int threadNumber)
            {
                Pool = pool;
                ThreadNumber = threadNumber;
            }
        }

        private readonly List<Thread> _threads = new();
        private int _runningThreadCount = 0;
        private object UserThreadParam { get; set; }
        private UserThreadThread UserThreadProc { get; set; }
        private FixedSizeWaitQueue<T> Queue { get; set; }

        public delegate void UserThreadThread(ThreadPoolQueue<T, P> pool, P? obj);
        public Exception? Exception { get; set; } = null;
        public bool HasException => Exception != null;
        public bool ContinueToProcessQueue { get; set; } = true;

        public int ThreadCount { get; private set; }
        public int QueueSize { get; private set; }
        public string PoolName { get; private set; }

        public ThreadPoolQueue(string poolName, UserThreadThread userThreadProc, object userThreadParam, int threadCount, int queueSize)
        {
            PoolName = poolName;
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

        public static ThreadPoolQueue<T, P> Create(string poolName, UserThreadThread userThreadProc, object userThreadParam, int threadCount)
        {
            var pool = new ThreadPoolQueue<T, P>(poolName, userThreadProc, userThreadParam, threadCount, threadCount * 10);

            return pool;
        }

        public static ThreadPoolQueue<T, P> Create(string poolName, UserThreadThread userThreadProc, object userThreadParam, int threadCount, int queueSize)
        {
            var pool = new ThreadPoolQueue<T, P>(poolName, userThreadProc, userThreadParam, threadCount, queueSize);

            return pool;
        }

        public static ThreadPoolQueue<T, P> CreateAndStart(string poolName, UserThreadThread userThreadProc, object userThreadParam, int threadCount, int queueSize)
        {
            var pool = new ThreadPoolQueue<T, P>(poolName, userThreadProc, userThreadParam, threadCount, queueSize);
            pool.Start();
            return pool;
        }

        public static ThreadPoolQueue<T, P> CreateAndStart(string poolName, UserThreadThread userThreadProc, object userThreadParam, int threadCount)
        {
            var pool = new ThreadPoolQueue<T, P>(poolName, userThreadProc, userThreadParam, threadCount, threadCount * 10);
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
                thread.Start(new PassThroughThreadParam<T, P>(this, i));
            }
        }

        private static void PassThroughThreadProc(object? param)
        {
            var threadParam = param as PassThroughThreadParam<T, P>;
            KbUtility.EnsureNotNull(threadParam);

            try
            {
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbPool:{threadParam.Pool.PoolName}:{threadParam.ThreadNumber}";
                threadParam.Pool.UserThreadProc(threadParam.Pool, (P)threadParam.Pool.UserThreadParam);
            }
            catch (Exception ex)
            {
                threadParam.Pool.Exception = ex;
                threadParam.Pool.ContinueToProcessQueue = false;
                threadParam.Pool.Queue.Stop();
            }
            finally
            {
                threadParam.Pool.DecrementRunningThreadCount();
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
