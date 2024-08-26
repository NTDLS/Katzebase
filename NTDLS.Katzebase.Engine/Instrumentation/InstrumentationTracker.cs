using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Semaphore;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Instrumentation
{
    internal class InstrumentationTracker
    {
        public bool Enabled { get; private set; }

        private readonly OptimisticCriticalResource<Dictionary<string, KbMetric>> _metrics = new();

        internal enum DiscretePerformanceCounter
        {
            ThreadCount,
            TransactionDuration
        }

        internal enum PerformanceCounter
        {
            IndexSearch,
            IndexDistillation,
            DocumentPointerUnion,
            DocumentPointerIntersect,
            AcquireTransaction,
            Optimization,
            Lock,
            Sampling,
            CacheRead,
            DeferredWrite,
            DeferredRead,
            CacheWrite,
            IORead,
            Compress,
            Decompress,
            IOWrite,
            Serialize,
            Deserialize,
            GetPBuf,
            ThreadCreation,
            ThreadQueue,
            ThreadDeQueue,
            ThreadReady,
            ThreadCompletion,
            Sorting,
            Evaluate,
            Rollback,
            Commit,
            Recording
        }

        public InstrumentationTracker(bool enableInstrumentation)
        {
            Enabled = enableInstrumentation;
        }

        #region Delegates.

        public delegate void DurationTrackerProc();
        public delegate T DurationTrackerProc<T>();
        public delegate T? DurationTrackerNullableProc<T>();

        /// <summary>
        /// Records performance of the given delegate for the given performance counter.
        /// </summary>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <param name="proc">Delegate function for which the duration will be recorded.</param>
        public void Measure(PerformanceCounter counter, DurationTrackerProc proc)
        {
            if (Enabled == false)
            {
                proc();
                return;
            }

            var tracker = CreateToken(counter);
            proc();
            tracker?.StopAndAccumulate();
        }

        /// <summary>
        /// Records performance of the given delegate for the given performance counter and name of supplied SubCategoryT type,
        /// </summary>
        /// <typeparam name="SubCategoryT">Type whose name will be used as the sub-category of the given performance counter.</typeparam>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <param name="proc">Delegate function for which the duration will be recorded.</param>
        public void Instrument<SubCategoryT>(PerformanceCounter counter, DurationTrackerProc proc)
        {
            if (Enabled == false)
            {
                proc();
                return;
            }

            var tracker = CreateToken<SubCategoryT>(counter);
            proc();
            tracker?.StopAndAccumulate();
        }

        /// <summary>
        /// Records performance of the given delegate for the given performance counter and text of the supplied subCategory.
        /// </summary>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <typeparam name="subCategory">Text will be used as the sub-category of the given performance counter.</typeparam>
        /// <param name="proc">Delegate function for which the duration will be recorded.</param>
        public void Measure(PerformanceCounter counter, string subCategory, DurationTrackerProc proc)
        {
            if (Enabled == false)
            {
                proc();
                return;
            }

            var tracker = CreateToken(counter, subCategory);
            proc();
            tracker?.StopAndAccumulate();
        }

        /// <summary>
        /// Records performance of the given delegate for the given performance counter.
        /// </summary>
        /// <typeparam name="ResultT">The type which will be returned to the calling caller.</typeparam>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <param name="proc">Delegate function for which the duration will be recorded.</param>
        /// <returns>The result from the given delegate.</returns>
        public ResultT Measure<ResultT>(PerformanceCounter counter, DurationTrackerProc<ResultT> proc)
        {
            if (Enabled == false)
            {
                return proc();
            }

            var tracker = CreateToken(counter);
            var result = proc();
            tracker?.StopAndAccumulate();
            return result;
        }

        /// <summary>
        /// Records performance of the given delegate for the given performance counter and name of supplied SubCategoryT type,
        /// </summary>
        /// <typeparam name="ResultT">The type which will be returned to the calling caller.</typeparam>
        /// <typeparam name="SubCategoryT">Type whose name will be used as the sub-category of the given performance counter.</typeparam>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <param name="proc">Delegate function for which the duration will be recorded.</param>
        /// <returns>The result from the given delegate.</returns>
        public ResultT Measure<ResultT, SubCategoryT>(PerformanceCounter counter, DurationTrackerProc<ResultT> proc)
        {
            if (Enabled == false)
            {
                return proc();
            }

            var tracker = CreateToken<SubCategoryT>(counter);
            var result = proc();
            tracker?.StopAndAccumulate();
            return result;
        }

        /// <summary>
        /// Records performance of the given delegate for the given performance counter and text of the supplied subCategory.
        /// </summary>
        /// <typeparam name="ResultT">The type which will be returned to the calling caller.</typeparam>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <typeparam name="subCategory">Text will be used as the sub-category of the given performance counter.</typeparam>
        /// <param name="proc">Delegate function for which the duration will be recorded.</param>
        /// <returns>The result from the given delegate.</returns>
        public ResultT Measure<ResultT>(PerformanceCounter counter, string subCategory, DurationTrackerProc<ResultT> proc)
        {
            if (Enabled == false)
            {
                return proc();
            }

            var tracker = CreateToken(counter, subCategory);
            var result = proc();
            tracker?.StopAndAccumulate();
            return result;
        }

        /// <summary>
        /// Records performance of the given delegate for the given performance counter.
        /// </summary>
        /// <typeparam name="ResultT">The type which will be returned to the calling caller.</typeparam>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <param name="proc">Delegate function for which the duration will be recorded.</param>
        /// <returns>The result from the given delegate.</returns>
        public ResultT? MeasureNullable<ResultT>(PerformanceCounter counter, DurationTrackerNullableProc<ResultT> proc)
        {
            if (Enabled == false)
            {
                return proc();
            }

            var tracker = CreateToken(counter);
            var result = proc();
            tracker?.StopAndAccumulate();
            return result;
        }

        /// <summary>
        /// Records performance of the given delegate for the given performance counter and name of supplied SubCategoryT type,
        /// </summary>
        /// <typeparam name="ResultT">The type which will be returned to the calling caller.</typeparam>
        /// <typeparam name="SubCategoryT">Type whose name will be used as the sub-category of the given performance counter.</typeparam>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <param name="proc">Delegate function for which the duration will be recorded.</param>
        /// <returns>The result from the given delegate.</returns>
        public ResultT? MeasureNullable<ResultT, SubCategoryT>(PerformanceCounter counter, DurationTrackerNullableProc<ResultT> proc)
        {
            if (Enabled == false)
            {
                return proc();
            }

            var tracker = CreateToken<SubCategoryT>(counter);
            var result = proc();
            tracker?.StopAndAccumulate();
            return result;
        }

        /// <summary>
        /// Records performance of the given delegate for the given performance counter and text of the supplied subCategory.
        /// </summary>
        /// <typeparam name="ResultT">The type which will be returned to the calling caller.</typeparam>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <typeparam name="subCategory">Text will be used as the sub-category of the given performance counter.</typeparam>
        /// <param name="proc">Delegate function for which the duration will be recorded.</param>
        /// <returns>The result from the given delegate.</returns>
        public ResultT? MeasureNullable<ResultT>(PerformanceCounter counter, string subCategory, DurationTrackerNullableProc<ResultT> proc)
        {
            if (Enabled == false)
            {
                return proc();
            }

            var tracker = CreateToken(counter, subCategory);
            var result = proc();
            tracker?.StopAndAccumulate();
            return result;
        }

        #endregion

        /// <summary>
        /// Creates an instrumentation token which can be used to record the duration of various actions.
        /// StopAndAccumulate should be called on the token when finished.
        /// </summary>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <returns></returns>
        internal InstrumentationDurationToken? CreateToken(PerformanceCounter counter)
            => Enabled ? new(this, counter, $"{counter}") : null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <typeparam name="subCategory">Text will be used as the sub-category of the given performance counter.</typeparam>
        /// <returns>Token which is used to track time and via a call to StopAndAccumulate when finished.</returns>
        internal InstrumentationDurationToken? CreateToken(PerformanceCounter counter, string subCategory)
            => Enabled ? new(this, counter, $"{counter}:{subCategory}") : null;

        /// <summary>
        /// Records performance of the given delegate for the given performance counter and name of supplied SubCategoryT type,
        /// </summary>
        /// <typeparam name="SubCategoryT">Type whose name will be used as the sub-category of the given performance counter.</typeparam>
        /// <param name="counter">The performance category for instrumentation.</param>
        /// <returns>Token which is used to track time and via a call to StopAndAccumulate when finished.</returns>
        internal InstrumentationDurationToken? CreateToken<SubCategoryT>(PerformanceCounter counter)
            => Enabled ? new(this, counter, $"{counter}:{typeof(SubCategoryT).Name}") : null;

        /// <summary>
        /// Records the trace token duration to the instrumentation collection.
        /// Can be called directly, but is typically called via InstrumentationDurationToken.StopAndAccumulate()
        /// </summary>
        /// <param name="item"></param>
        public void AccumulateDuration(InstrumentationDurationToken item)
        {
            _metrics.Write(o =>
            {
                if (o.TryGetValue(item.Key, out var metric))
                {
                    metric.Value += item.Duration;
                    metric.Count++;
                }
                else
                {
                    o.Add(item.Key, new KbMetric(KbMetricType.Cumulative, item.Key, item.Duration) { Count = 1 });
                }
            });
        }

        /// <summary>
        /// Adds a discrete performance metric to the instrumentation collection. These can be any numbers that are not accumulated.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="eventValue"></param>
        public void AddDiscreteMetric(DiscretePerformanceCounter type, double eventValue)
        {
            _metrics.Write(o =>
            {
                var key = $"{type}";

                if (o.TryGetValue(key, out var metric))
                {
                    metric.Value = eventValue;
                    metric.Count++;
                }
                else
                {
                    o.Add(key, new KbMetric(KbMetricType.Discrete, key, eventValue) { Count = 1 });
                }
            });
        }

        internal KbMetricCollection ToCollection()
        {
            return _metrics.Read(o =>
            {
                var result = new KbMetricCollection();
                result.AddRange(o.Select(o => o.Value));
                return result;
            });
        }
    }
}
