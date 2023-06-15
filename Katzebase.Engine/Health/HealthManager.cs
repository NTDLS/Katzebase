using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Health
{
    public class HealthManager
    {
        public Dictionary<string, HealthCounter> Counters;

        private Core core;
        private DateTime lastCheckpoint = DateTime.MinValue;

        public HealthManager(Core core)
        {
            this.core = core;

            string healthCounterDiskPath = Path.Combine(core.Settings.LogDirectory, HealthStatsFile);
            if (File.Exists(healthCounterDiskPath))
            {
                var physicalCounters = core.IO.GetJsonNonTracked<List<HealthCounter>>(healthCounterDiskPath, true);

                if (physicalCounters == null || physicalCounters.Count == 0)
                {
                    Counters = new Dictionary<string, HealthCounter>();
                }
                else
                {
                    Counters = physicalCounters.ToDictionary(o => o.Instance);
                }
            }
            else
            {
                Counters = new Dictionary<string, HealthCounter>();
            }
        }

        public void Close()
        {
            Checkpoint();
        }

        public void Checkpoint()
        {
            lock (Counters)
            {
                lastCheckpoint = DateTime.UtcNow;
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

                core.IO.PutJsonNonTracked(Path.Combine(core.Settings.LogDirectory, HealthStatsFile), physicalCounters, true);
            }
        }

        /// <summary>
        /// Increment the specified counter by a defined amount.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Increment(HealthCounterType type, double value)
        {
            if (value == 0 || core.Settings.HealthMonitoringEnabled == false)
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

                if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds >= core.Settings.HealthMonitoringChekpointSeconds)
                {
                    Checkpoint();
                }
            }
        }

        /// <summary>
        /// Increment the specified counter by a defined amount.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Increment(HealthCounterType type, string instance, double value)
        {
            if (value == 0 || core.Settings.HealthMonitoringEnabled == false || core.Settings.HealthMonitoringInstanceLevelEnabled == false)
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

                if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > core.Settings.HealthMonitoringChekpointSeconds)
                {
                    Checkpoint();
                }
            }
        }

        /// <summary>
        /// Increment the specified counter by 1.
        /// </summary>
        /// <param name="type"></param>
        public void Increment(HealthCounterType type)
        {
            Increment(type, 1);
        }

        /// <summary>
        /// Increment the specified counter by 1.
        /// </summary>
        /// <param name="type"></param>
        public void Increment(HealthCounterType type, string instance)
        {
            Increment(type, instance, 1);
        }

        /// <summary>
        /// Set the specified counter to the defined value.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Set(HealthCounterType type, Int64 value)
        {
            if (core.Settings.HealthMonitoringEnabled == false)
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

                if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > core.Settings.HealthMonitoringChekpointSeconds)
                {
                    Checkpoint();
                }
            }
        }

        /// <summary>
        /// Set the specified counter to the defined value.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Set(HealthCounterType type, string instance, double value)
        {
            if (value == 0 || core.Settings.HealthMonitoringEnabled == false || core.Settings.HealthMonitoringInstanceLevelEnabled == false)
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

                if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > core.Settings.HealthMonitoringChekpointSeconds)
                {
                    Checkpoint();
                }
            }
        }
    }
}
