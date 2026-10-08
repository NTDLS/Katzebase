using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Health;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using System.Collections.Concurrent;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to health.
    /// </summary>
    public class HealthManager
    {
        /// <summary>
        /// Counters are incremented on hot paths (every IO read, cache hit, lock grant, etc.) so they must not share a
        /// single global write lock. The dictionary is concurrent and each counter is updated under its own lock.
        /// </summary>
        private readonly ConcurrentDictionary<string, HealthCounter> _counters = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly EngineCore _core;
        private long _lastCheckpointTicks = DateTime.MinValue.Ticks;
        private int _isCheckpointing = 0;

        internal HealthQueryHandlers QueryHandlers { get; private set; }
        public HealthAPIHandlers APIHandlers { get; private set; }

        internal HealthManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new HealthQueryHandlers(core);
                APIHandlers = new HealthAPIHandlers(core);

                string healthCounterDiskPath = Path.Combine(core.Settings.LogDirectory, HealthStatsFile);
                if (File.Exists(healthCounterDiskPath))
                {
                    var physicalCounters = IOManager.GetJsonNonTracked<KbInsensitiveDictionary<HealthCounter>>(healthCounterDiskPath);

                    if (physicalCounters != null)
                    {
                        foreach (var kvp in physicalCounters)
                        {
                            _counters[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate health manager.", ex);
                throw;
            }
        }

        public void Stop()
        {
            Checkpoint();
        }

        public KbInsensitiveDictionary<HealthCounter> CloneCounters()
        {
            var clone = new KbInsensitiveDictionary<HealthCounter>();
            foreach (var kvp in _counters)
            {
                lock (kvp.Value)
                {
                    clone[kvp.Key] = new HealthCounter()
                    {
                        Count = kvp.Value.Count,
                        Value = kvp.Value.Value,
                        Timestamp = kvp.Value.Timestamp
                    };
                }
            }
            return clone;
        }

        public void ClearCounters()
        {
            _counters.Clear();
            Checkpoint();
        }

        public void Checkpoint()
        {
            try
            {
                Interlocked.Exchange(ref _lastCheckpointTicks, DateTime.UtcNow.Ticks);
                IOManager.PutJsonNonTrackedPretty(Path.Combine(_core.Settings.LogDirectory, HealthStatsFile), CloneCounters());
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to checkpoint health manager.", ex);
                throw;
            }
        }

        /// <summary>
        /// Checkpoints the counters if the checkpoint interval has elapsed. Only one thread checkpoints at a time
        /// and no counter locks are held while doing so.
        /// </summary>
        private void CheckpointIfDue()
        {
            var lastCheckpoint = new DateTime(Interlocked.Read(ref _lastCheckpointTicks));
            if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds >= _core.Settings.HealthMonitoringCheckpointSeconds
                && Interlocked.CompareExchange(ref _isCheckpointing, 1, 0) == 0)
            {
                try
                {
                    Checkpoint();
                }
                finally
                {
                    Volatile.Write(ref _isCheckpointing, 0);
                }
            }
        }

        /// <param name="isDiscrete">Discrete counters only count their first occurrence (Value holds the number of occurrences).</param>
        private void Accumulate(string key, double value, bool isDiscrete)
        {
            var counter = _counters.GetOrAdd(key, _ => new HealthCounter());
            lock (counter)
            {
                counter.Value += value;
                if (isDiscrete == false || counter.Count == 0)
                {
                    counter.Count++;
                }
                counter.Timestamp = DateTime.UtcNow;
            }

            CheckpointIfDue();
        }

        /// <summary>
        /// Increment the specified counter by a defined amount, typically used for tings like duration.
        /// </summary>
        public void IncrementContinuous(HealthCounterType type, double perfValue)
        {
            try
            {
                if (perfValue == 0 || _core.Settings.HealthMonitoringEnabled == false)
                {
                    return;
                }

                Accumulate(type.ToString(), perfValue, false);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed.", ex);
                throw;
            }
        }

        /// <summary>
        /// Increment the specified counter by a defined amount, typically used for tings like duration.
        /// </summary>
        public void IncrementContinuous(HealthCounterType type, string instance, double perfValue)
        {
            try
            {
                if (perfValue == 0 || _core.Settings.HealthMonitoringEnabled == false || _core.Settings.HealthMonitoringInstanceLevelEnabled == false)
                {
                    return;
                }

                Accumulate($"{type}:{instance}", perfValue, false);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed.", ex);
                throw;
            }
        }

        /// <summary>
        /// Increments a discrete metric by 1, this is typically used to track the counts or occurrences.
        /// </summary>
        public void IncrementDiscrete(HealthCounterType type)
        {
            try
            {
                if (_core.Settings.HealthMonitoringEnabled == false)
                {
                    return;
                }

                Accumulate(type.ToString(), 1, true);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed.", ex);
                throw;
            }
        }

        /// <summary>
        /// Increments a discrete metric by 1, this is typically used to track the counts or occurrences.
        /// </summary>
        public void IncrementDiscrete(HealthCounterType type, string instance)
        {
            try
            {
                if (_core.Settings.HealthMonitoringEnabled == false || _core.Settings.HealthMonitoringInstanceLevelEnabled == false)
                {
                    return;
                }

                Accumulate($"{type}:{instance}", 1, true);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed.", ex);
                throw;
            }
        }
    }
}
