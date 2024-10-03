using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Health;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Semaphore;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to health.
    /// </summary>
    public class HealthManager<TData> where TData : IStringable
    {
        internal OptimisticCriticalResource<KbInsensitiveDictionary<HealthCounter>> Counters { get; private set; } = new();

        private readonly EngineCore<TData> _core;
        private DateTime lastCheckpoint = DateTime.MinValue;

        internal HealthQueryHandlers<TData> QueryHandlers { get; private set; }
        public HealthAPIHandlers<TData> APIHandlers { get; private set; }

        internal HealthManager(EngineCore<TData> core)
        {
            _core = core;

            try
            {
                QueryHandlers = new HealthQueryHandlers<TData>(core);
                APIHandlers = new HealthAPIHandlers<TData>(core);

                string healthCounterDiskPath = Path.Combine(core.Settings.LogDirectory, HealthStatsFile);
                if (File.Exists(healthCounterDiskPath))
                {
                    var physicalCounters = core.IO.GetJsonNonTracked<KbInsensitiveDictionary<HealthCounter>>(healthCounterDiskPath);

                    if (physicalCounters != null)
                    {
                        Counters.Write(o => physicalCounters.ToList().ForEach(kvp => o.Add(kvp.Key, kvp.Value)));
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
            return Counters.Read(o => o.Clone());
        }

        public void ClearCounters()
        {
            Counters.Write(o => o.Clear());
            Checkpoint();
        }

        public void Checkpoint()
        {
            try
            {
                lastCheckpoint = DateTime.UtcNow;

                Counters.Read(o =>
                {
                    _core.IO.PutJsonNonTrackedPretty(Path.Combine(_core.Settings.LogDirectory, HealthStatsFile), o);
                });
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to checkpoint health manager.", ex);
                throw;
            }
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

                string key = type.ToString();

                Counters.Write(o =>
                {
                    if (o.TryGetValue(key, out HealthCounter? value))
                    {
                        var counterItem = value;
                        counterItem.Value += perfValue;
                        counterItem.Count++;
                        counterItem.Timestamp = DateTime.UtcNow;
                    }
                    else
                    {
                        o.Add(key, new HealthCounter()
                        {
                            Value = perfValue,
                            Count = 1,
                            Timestamp = DateTime.UtcNow
                        });
                    }

                    if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds >= _core.Settings.HealthMonitoringCheckpointSeconds)
                    {
                        Checkpoint();
                    }
                });
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to increment continuous health counter.", ex);
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

                string key = $"{type}:{instance}";

                Counters.Write(o =>
                {
                    if (o.TryGetValue(key, out HealthCounter? value))
                    {
                        var counterItem = value;
                        counterItem.Value += perfValue;
                        counterItem.Count++;
                        counterItem.Timestamp = DateTime.UtcNow;
                    }
                    else
                    {
                        o.Add(key, new HealthCounter()
                        {
                            Value = perfValue,
                            Count = 1,
                            Timestamp = DateTime.UtcNow
                        });
                    }

                    if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > _core.Settings.HealthMonitoringCheckpointSeconds)
                    {
                        Checkpoint();
                    }
                });
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to increment continuous health counter.", ex);
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

                string key = type.ToString();

                Counters.Write(o =>
                {
                    if (o.TryGetValue(key, out HealthCounter? value))
                    {
                        var counterItem = value;
                        counterItem.Value += 1;
                        counterItem.Timestamp = DateTime.UtcNow;
                    }
                    else
                    {
                        o.Add(key, new HealthCounter()
                        {
                            Value = 1,
                            Count = 1,
                            Timestamp = DateTime.UtcNow
                        });
                    }

                    if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds >= _core.Settings.HealthMonitoringCheckpointSeconds)
                    {
                        Checkpoint();
                    }
                });
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to increment discrete health counter.", ex);
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

                string key = $"{type}:{instance}";

                Counters.Write(o =>
                {
                    if (o.TryGetValue(key, out HealthCounter? value))
                    {
                        var counterItem = value;
                        counterItem.Value += 1;
                        counterItem.Timestamp = DateTime.UtcNow;
                    }
                    else
                    {
                        o.Add(key, new HealthCounter()
                        {
                            Value = 1,
                            Count = 1,
                            Timestamp = DateTime.UtcNow
                        });
                    }

                    if ((DateTime.UtcNow - lastCheckpoint).TotalSeconds > _core.Settings.HealthMonitoringCheckpointSeconds)
                    {
                        Checkpoint();
                    }
                });
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to increment discrete health counter.", ex);
                throw;
            }
        }
    }
}
