using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Types;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Health.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to health.
    /// </summary>
    public class HealthManager
    {
        public KbInsensitiveDictionary<HealthCounter> Counters { get; private set; } = new();

        private readonly Core _core;
        private DateTime lastCheckpoint = DateTime.MinValue;

        internal HealthQueryHandlers QueryHandlers { get; private set; }
        public HealthAPIHandlers APIHandlers { get; private set; }

        public HealthManager(Core core)
        {
            _core = core;

            try
            {
                QueryHandlers = new HealthQueryHandlers(core);
                APIHandlers = new HealthAPIHandlers(core);

                string healthCounterDiskPath = Path.Combine(core.Settings.LogDirectory, HealthStatsFile);
                if (File.Exists(healthCounterDiskPath))
                {
                    var physicalCounters = core.IO.GetJsonNonTracked<KbInsensitiveDictionary<HealthCounter>>(healthCounterDiskPath, false);
                    if (physicalCounters != null)
                    {
                        Counters = physicalCounters;
                    }
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate health manager.", ex);
                throw;
            }
        }

        public void Close()
        {
            Checkpoint();
        }

        public KbInsensitiveDictionary<HealthCounter> CloneCounters()
        {
            lock (Counters)
            {
                return Counters.Clone();
            }
        }

        public void ClearCounters()
        {
            lock (Counters)
            {
                Counters.Clear();
                Checkpoint();
            }
        }

        public void Checkpoint()
        {
            try
            {
                lock (Counters)
                {
                    lastCheckpoint = DateTime.UtcNow;
                    /* TODO: Cleanup the counters - can't keep them forever.
                    var physicalCounters = Counters.Values.Where(o => o.Value > 0).ToList();

                    //All counters have a non-null instance because we use it for a key, but the ones that are really per-instance
                    //  will have an instance different than the type. Here we want to find the most recent instance counter and
                    //  remove any other instance counters that are n-seconds older than it is. Otherwise this grows forever.
                    //  As for the non-instance counters, we leave those forever. The user can clear them as they see fit.
                    var instanceCounters = physicalCounters.Where(o => o.Instance == o.Type.ToString());
                    if (instanceCounters.Any())
                    {
                        var mostRecentCounter = instanceCounters.Max(o => o.WaitDateTimeUtc);
                        var itemsToRemove = physicalCounters.Where(o => o.Instance == o.Type.ToString()
                                        && (mostRecentCounter - o.WaitDateTimeUtc).TotalSeconds > core.Settings.HealthMonitoringInstanceLevelTimeToLiveSeconds).ToList();

                        foreach (var itemToRemove in itemsToRemove)
                        {
                            physicalCounters.Remove(itemToRemove);
                            Counters.Remove(itemToRemove.Instance);
                        }
                    }
                    */

                    _core.IO.PutJsonNonTracked(Path.Combine(_core.Settings.LogDirectory, HealthStatsFile), Counters, false);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to checkpoint health manager.", ex);
                throw;
            }

        }

        /// <summary>
        /// Increment the specified counter by a defined amount.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Increment(HealthCounterType type, double value)
        {
            try
            {
                if (value == 0 || _core.Settings.HealthMonitoringEnabled == false)
                {
                    return;
                }

                string key = type.ToString();

                lock (Counters)
                {
                    if (Counters.ContainsKey(key))
                    {
                        var counterItem = Counters[key];
                        counterItem.Value += value;
                        counterItem.WaitDateTimeUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        Counters.Add(key, new HealthCounter()
                        {
                            Instance = key,
                            Type = type,
                            Value = value,
                            WaitDateTimeUtc = DateTime.UtcNow
                        });
                    }

                    if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds >= _core.Settings.HealthMonitoringChekpointSeconds)
                    {
                        Checkpoint();
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to increment health counter.", ex);
                throw;
            }
        }

        /// <summary>
        /// Increment the specified counter by a defined amount.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Increment(HealthCounterType type, string instance, double value)
        {
            try
            {
                if (value == 0 || _core.Settings.HealthMonitoringEnabled == false || _core.Settings.HealthMonitoringInstanceLevelEnabled == false)
                {
                    return;
                }

                string key = $"{type}:{instance}";

                lock (Counters)
                {
                    if (Counters.ContainsKey(key))
                    {
                        var counterItem = Counters[key];
                        counterItem.Value += value;
                        counterItem.WaitDateTimeUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        Counters.Add(key, new HealthCounter()
                        {
                            Instance = key,
                            Type = type,
                            Value = value,
                            WaitDateTimeUtc = DateTime.UtcNow
                        });
                    }

                    if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > _core.Settings.HealthMonitoringChekpointSeconds)
                    {
                        Checkpoint();
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to increment health counter.", ex);
                throw;
            }
        }

        /// <summary>
        /// Increment the specified counter by 1.
        /// </summary>
        /// <param name="type"></param>
        public void Increment(HealthCounterType type) => Increment(type, 1);

        /// <summary>
        /// Increment the specified counter by 1.
        /// </summary>
        /// <param name="type"></param>
        public void Increment(HealthCounterType type, string instance) => Increment(type, instance, 1);

        /// <summary>
        /// Set the specified counter to the defined value.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Set(HealthCounterType type, long value)
        {
            try
            {
                if (_core.Settings.HealthMonitoringEnabled == false)
                {
                    return;
                }

                string key = $"{type}";

                lock (Counters)
                {
                    if (Counters.ContainsKey(key))
                    {
                        var counterItem = Counters[key];
                        counterItem.Value = value;
                        counterItem.WaitDateTimeUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        Counters.Add(key, new HealthCounter()
                        {
                            Instance = key,
                            Type = type,
                            Value = value,
                            WaitDateTimeUtc = DateTime.UtcNow
                        });
                    }

                    if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > _core.Settings.HealthMonitoringChekpointSeconds)
                    {
                        Checkpoint();
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to set health counter.", ex);
                throw;
            }
        }

        /// <summary>
        /// Set the specified counter to the defined value.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Set(HealthCounterType type, string instance, double value)
        {
            try
            {
                if (value == 0 || _core.Settings.HealthMonitoringEnabled == false || _core.Settings.HealthMonitoringInstanceLevelEnabled == false)
                {
                    return;
                }

                string key = $"{type}:{instance}";

                lock (Counters)
                {
                    if (Counters.ContainsKey(key))
                    {
                        var counterItem = Counters[key];
                        counterItem.Value = value;
                        counterItem.WaitDateTimeUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        Counters.Add(key, new HealthCounter()
                        {
                            Instance = key,
                            Type = type,
                            Value = value,
                            WaitDateTimeUtc = DateTime.UtcNow
                        });
                    }

                    if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > _core.Settings.HealthMonitoringChekpointSeconds)
                    {
                        Checkpoint();
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to set health counter.", ex);
                throw;
            }
        }
    }
}
